using System.Text.Json;
using System.Text.Json.Serialization;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.Integracoes.Relatorios;
using Refit;

namespace Concessionaria.Carros.Integracoes;

public static class ExtensoesIntegracoes
{
    private static readonly RefitSettings ConfiguracaoRefit = new(new SystemTextJsonContentSerializer(
        new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } }));

    public static IServiceCollection AdicionarIntegracoes(this IServiceCollection services, IConfiguration configuration)
    {
        var configuracao = configuration.GetRequiredSection(ConfiguracaoIntegracoes.Secao).Get<ConfiguracaoIntegracoes>()!;

        services.AddTransient<PropagadorIdOperacaoHandler>();

        // As chamadas a Relatórios são cálculos sem efeito colateral, então o retry vale também para POST.
        services.AddRefitGeneratedClient<IRelatoriosApi>(ConfiguracaoRefit, httpClientName: "relatorios")
            .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(configuracao.RelatoriosApi))
            .AddHttpMessageHandler<PropagadorIdOperacaoHandler>()
            .AddStandardResilienceHandler(opcoes =>
            {
                opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(configuracao.TimeoutTentativaSegundos);
                opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(configuracao.TimeoutTotalSegundos);
                opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, configuracao.TimeoutTentativaSegundos * 2));
            });

        return services;
    }
}
