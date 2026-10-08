using System.Text.Json;
using System.Text.Json.Serialization;
using Concessionaria.Bff.Comum.Observabilidade;
using Concessionaria.Bff.Infraestrutura.Configuracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Microsoft.Extensions.Http.Resilience;
using Polly.Timeout;
using Refit;

namespace Concessionaria.Bff.Infraestrutura;

/// <summary>Registro da camada de infraestrutura: clientes Refit de Carros e Motos com resiliência.</summary>
public static class ExtensoesInfraestrutura
{
    private static readonly RefitSettings ConfiguracaoRefit = new(new SystemTextJsonContentSerializer(
        new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } }));

    /// <summary>Registra os clientes de Carros ("carros") e Motos ("motos") com propagação do Id de operação.</summary>
    public static IServiceCollection AdicionarIntegracoes(this IServiceCollection services, IConfiguration configuration)
    {
        var configuracao = configuration.GetRequiredSection(ConfiguracaoIntegracoes.Secao).Get<ConfiguracaoIntegracoes>()!;

        services.AddTransient<PropagadorIdOperacaoHandler>();
        AdicionarCliente<ICarrosApi>(services, "carros", configuracao.CarrosApi, configuracao);
        AdicionarCliente<IMotosApi>(services, "motos", configuracao.MotosApi, configuracao);

        return services;
    }

    /// <summary>
    /// Registra um cliente Refit com timeout, retry e circuit breaker. O nome do cliente vira o rótulo
    /// <c>pipeline_name</c> ("{nome}-standard") nas métricas de resiliência e identifica o destino nos dashboards.
    /// </summary>
    private static void AdicionarCliente<TApi>(IServiceCollection services, string nomeCliente, string urlBase, ConfiguracaoIntegracoes configuracao) where TApi : class =>
        services.AddRefitGeneratedClient<TApi>(ConfiguracaoRefit, httpClientName: nomeCliente)
            .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(urlBase))
            .AddHttpMessageHandler<PropagadorIdOperacaoHandler>()
            .AddStandardResilienceHandler(opcoes =>
            {
                opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(configuracao.TimeoutTentativaSegundos);
                opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(configuracao.TimeoutTotalSegundos);
                opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, configuracao.TimeoutTentativaSegundos * 2));
                // Retry só para falha de transporte (conexão recusada, timeout da tentativa). Um 5xx de Carros/Motos
                // já é o resultado depois dos retries deles contra Relatórios; repetir aqui multiplicaria as tentativas
                // em cascata e mascararia o erro original com um timeout do BFF.
                opcoes.Retry.ShouldHandle = argumentos => ValueTask.FromResult(EhFalhaDeTransporte(argumentos.Outcome.Exception));
                // O circuito também só abre por falha de transporte, ou seja, quando Carros/Motos está inalcançável. Com o
                // padrão (que conta 5xx), os 502 das planilhas com Relatórios fora abriam o circuito para Carros inteiro,
                // e rotas saudáveis (listagens, vendas, painel) passavam a responder 503: falha em cascata vista no
                // cenário relatorios-fora.
                opcoes.CircuitBreaker.ShouldHandle = argumentos => ValueTask.FromResult(EhFalhaDeTransporte(argumentos.Outcome.Exception));
                // Sem retry em POST/PUT/DELETE: repetir uma venda que chegou a ser gravada geraria erro de "já vendido".
                opcoes.Retry.DisableForUnsafeHttpMethods();
            });

    /// <summary>Falha de transporte: o serviço não respondeu (conexão recusada ou timeout da tentativa).</summary>
    private static bool EhFalhaDeTransporte(Exception? excecao) =>
        excecao is HttpRequestException or TimeoutRejectedException;
}
