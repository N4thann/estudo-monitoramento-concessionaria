using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Concessionaria.Bff.Comum.Observabilidade;

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
    /// Traces: ASP.NET Core e HttpClient (cada retry vira span), exportados por OTLP.
    /// Métricas: ASP.NET Core, HttpClient, runtime, Polly e de negócio (painel degradado), expostas para o Prometheus.
    /// </summary>
    public static WebApplicationBuilder AdicionarTelemetria(this WebApplicationBuilder builder, string nomeServico)
    {
        var configuracao = builder.Configuration.ObterConfiguracaoTelemetria();
        Telemetria.CriarMedidorLimiteMemoria();

        builder.Services.AddOpenTelemetry()
            // Resource: "quem sou eu" — vai junto de todo span e métrica.
            .ConfigureResource(recurso => recurso
                .AddService(serviceName: nomeServico, serviceNamespace: "concessionaria", serviceVersion: "1.0.0")
                .AddAttributes([new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(Telemetria.Nome)   // spans próprios do serviço
                    .AddAspNetCoreInstrumentation(opcoes =>
                    {
                        // /metrics e /saude não viram trace: são chamadas de infraestrutura, não de usuário.
                        opcoes.Filter = contexto => !EhRotaInfraestrutura(contexto.Request.Path);
                        opcoes.RecordException = true;   // exceção vira evento dentro do span
                    })
                    .AddHttpClientInstrumentation(opcoes => opcoes.RecordException = true)
                    .AddOtlpExporter(opcoes => opcoes.Endpoint = new Uri(configuracao.EndpointOtlp));   // → Collector :4317

                if (configuracao.ExportarTracesNoConsole)
                    tracing.AddConsoleExporter();   // modo estudo: imprime os spans no console
            })
            .WithMetrics(metricas => metricas
                .AddMeter(Telemetria.Nome)        // métricas de negócio
                .AddAspNetCoreInstrumentation()   // http.server.request.duration
                .AddHttpClientInstrumentation()   // http.client.request.duration
                // CPU, memória, GC e threads do processo (medidor nativo do .NET).
                .AddMeter("System.Runtime")
                // Retries, timeouts e circuito aberto do AddStandardResilienceHandler.
                .AddMeter("Polly")
                .AddPrometheusExporter());        // publica em /metrics

        return builder;
    }

    /// <summary>Expõe as métricas em <see cref="RotaMetricas"/> para o scrape do Prometheus.</summary>
    public static WebApplication MapearMetricas(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint(RotaMetricas);
        return app;
    }
}
