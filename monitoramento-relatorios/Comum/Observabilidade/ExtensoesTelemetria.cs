using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Concessionaria.Relatorios.Comum.Observabilidade;

/// <summary>Traces e métricas com OpenTelemetry: instrumentação, exportação OTLP e endpoint do Prometheus.</summary>
public static class ExtensoesTelemetria
{
    /// <summary>Rota lida pelo Prometheus.</summary>
    public const string RotaMetricas = "/metrics";

    /// <summary>Rota do health check.</summary>
    public const string RotaSaude = "/saude";

    /// <summary>Scrape do Prometheus e health checks não entram em traces nem no log de requisições: são ruído, não uso real.</summary>
    public static bool EhRotaInfraestrutura(PathString caminho) =>
        caminho.StartsWithSegments(RotaMetricas) || caminho.StartsWithSegments(RotaSaude);

    /// <summary>Lê a seção <c>Telemetria</c>, com os valores padrão se ela não existir.</summary>
    public static ConfiguracaoTelemetria ObterConfiguracaoTelemetria(this IConfiguration configuration) =>
        configuration.GetSection(ConfiguracaoTelemetria.Secao).Get<ConfiguracaoTelemetria>() ?? new ConfiguracaoTelemetria();

    /// <summary>
    /// Traces: ASP.NET Core e spans próprios (geração de planilha), exportados por OTLP.
    /// Métricas: ASP.NET Core, runtime e de negócio, expostas para o Prometheus.
    /// </summary>
    public static WebApplicationBuilder AdicionarTelemetria(this WebApplicationBuilder builder, string nomeServico)
    {
        var configuracao = builder.Configuration.ObterConfiguracaoTelemetria();
        Telemetria.CriarMedidorLimiteMemoria();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(recurso => recurso
                .AddService(serviceName: nomeServico, serviceNamespace: "concessionaria", serviceVersion: "1.0.0")
                .AddAttributes([new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(Telemetria.Nome)
                    .AddAspNetCoreInstrumentation(opcoes =>
                    {
                        opcoes.Filter = contexto => !EhRotaInfraestrutura(contexto.Request.Path);
                        opcoes.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(opcoes => opcoes.RecordException = true)
                    .AddOtlpExporter(opcoes => opcoes.Endpoint = new Uri(configuracao.EndpointOtlp));

                if (configuracao.ExportarTracesNoConsole)
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metricas => metricas
                .AddMeter(Telemetria.Nome)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                // CPU, memória, GC e threads do processo (medidor nativo do .NET).
                .AddMeter("System.Runtime")
                .AddPrometheusExporter());

        return builder;
    }

    /// <summary>Expõe as métricas em <see cref="RotaMetricas"/> para o scrape do Prometheus.</summary>
    public static WebApplication MapearMetricas(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint(RotaMetricas);
        return app;
    }
}
