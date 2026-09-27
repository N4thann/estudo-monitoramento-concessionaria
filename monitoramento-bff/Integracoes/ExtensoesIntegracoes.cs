using System.Text.Json;
using System.Text.Json.Serialization;
using Concessionaria.Bff.Comum.Observabilidade;
using Microsoft.Extensions.Http.Resilience;
using Polly.Timeout;
using Refit;

namespace Concessionaria.Bff.Integracoes;

public static class ExtensoesIntegracoes
{
    private static readonly RefitSettings ConfiguracaoRefit = new(new SystemTextJsonContentSerializer(
        new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } }));

    public static IServiceCollection AdicionarIntegracoes(this IServiceCollection services, IConfiguration configuration)
    {
        var configuracao = configuration.GetRequiredSection(ConfiguracaoIntegracoes.Secao).Get<ConfiguracaoIntegracoes>()!;

        services.AddTransient<PropagadorIdOperacaoHandler>();
        AdicionarCliente<ICarrosApi>(services, "carros", configuracao.CarrosApi, configuracao);
        AdicionarCliente<IMotosApi>(services, "motos", configuracao.MotosApi, configuracao);

        return services;
    }

    // O nome do cliente vira o rótulo pipeline_name nas métricas de resiliência e identifica o destino nos dashboards.
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
                opcoes.Retry.ShouldHandle = argumentos => ValueTask.FromResult(
                    argumentos.Outcome.Exception is HttpRequestException or TimeoutRejectedException);
                // Sem retry em POST/PUT/DELETE: repetir uma venda que chegou a ser gravada geraria erro de "já vendido".
                opcoes.Retry.DisableForUnsafeHttpMethods();
            });
}
