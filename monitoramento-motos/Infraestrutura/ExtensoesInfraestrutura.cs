using System.Text.Json;
using System.Text.Json.Serialization;
using Concessionaria.Motos.Comum.Observabilidade;
using Concessionaria.Motos.Dominio.Interfaces;
using Concessionaria.Motos.Infraestrutura.Banco;
using Concessionaria.Motos.Infraestrutura.Configuracoes;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios;
using Concessionaria.Motos.Infraestrutura.Repositorios;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using Refit;

namespace Concessionaria.Motos.Infraestrutura;

/// <summary>Registro da camada de infraestrutura: MongoDB e cliente da API de Relatórios.</summary>
public static class ExtensoesInfraestrutura
{
    private static readonly RefitSettings ConfiguracaoRefit = new(new SystemTextJsonContentSerializer(
        new JsonSerializerOptions(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } }));

    /// <summary>Registra o cliente do Mongo, o contexto, os repositórios e o inicializador de índices.</summary>
    public static IServiceCollection AdicionarMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConfiguracaoMongoDb>(configuration.GetSection(ConfiguracaoMongoDb.Secao));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var configuracoesCliente = MongoClientSettings.FromConnectionString(
                sp.GetRequiredService<IOptions<ConfiguracaoMongoDb>>().Value.StringConexao);

            // O driver já gera spans OpenTelemetry nativamente; aqui só incluímos o texto do comando no span
            // (db.query.text) para identificar consultas lentas. Dados de estudo — em produção avaliar PII antes.
            configuracoesCliente.TracingOptions = new TracingOptions { QueryTextMaxLength = 1000 };

            return new MongoClient(configuracoesCliente);
        });

        services.AddSingleton(sp =>
        {
            var configuracao = sp.GetRequiredService<IOptions<ConfiguracaoMongoDb>>().Value;
            return new ContextoMongoDb(sp.GetRequiredService<IMongoClient>().GetDatabase(configuracao.NomeBanco));
        });

        services.AddScoped<IRepositorioMotos, RepositorioMotos>();
        services.AddScoped<IRepositorioVendas, RepositorioVendas>();
        services.AddHostedService<InicializadorMongoDb>();

        return services;
    }

    /// <summary>
    /// Registra o cliente Refit de Relatórios com propagação do Id de operação e resiliência (timeout, retry e
    /// circuit breaker). O nome "relatorios" vira o rótulo <c>pipeline_name</c> nas métricas do Polly.
    /// </summary>
    public static IServiceCollection AdicionarIntegracoes(this IServiceCollection services, IConfiguration configuration)
    {
        var configuracao = configuration.GetRequiredSection(ConfiguracaoIntegracoes.Secao).Get<ConfiguracaoIntegracoes>()!;

        services.AddTransient<PropagadorIdOperacaoHandler>();

        // As chamadas a Relatórios são cálculos sem efeito colateral, então o retry vale também para POST.
        services.AddRefitGeneratedClient<IRelatoriosApi>(ConfiguracaoRefit, httpClientName: "relatorios")
            .ConfigureHttpClient(cliente => cliente.BaseAddress = new Uri(configuracao.RelatoriosApi))   // http://127.0.0.1:5051/
            .AddHttpMessageHandler<PropagadorIdOperacaoHandler>()                                        // handler 1
            .AddStandardResilienceHandler(opcoes =>                                                      // handler 2
            {
                opcoes.AttemptTimeout.Timeout = TimeSpan.FromSeconds(configuracao.TimeoutTentativaSegundos);    // 3s por tentativa
                opcoes.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(configuracao.TimeoutTotalSegundos);   // 8s no total
                opcoes.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, configuracao.TimeoutTentativaSegundos * 2));
            });

        return services;
    }
}
