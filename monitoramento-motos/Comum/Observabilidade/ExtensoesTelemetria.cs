using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Concessionaria.Motos.Comum.Observabilidade;

public class ConfiguracaoTelemetria
{
    public const string Secao = "Telemetria";

    // OpenTelemetry Collector (OTLP/gRPC). Enquanto ele não estiver no ar, o envio falha em silêncio e a API segue normal.
    public string EndpointOtlp { get; set; } = "http://127.0.0.1:4317";

    // Útil para ver os spans sem nenhuma ferramenta rodando; desligado por padrão porque polui o console.
    public bool ExportarTracesNoConsole { get; set; }
}

public static class ExtensoesTelemetria
{
    public const string RotaMetricas = "/metrics";
    public const string RotaSaude = "/saude";

    // Scrape do Prometheus e health checks não entram em traces nem no log de requisições: são ruído, não uso real.
    public static bool EhRotaInfraestrutura(PathString caminho) =>
        caminho.StartsWithSegments(RotaMetricas) || caminho.StartsWithSegments(RotaSaude);

    public static ConfiguracaoTelemetria ObterConfiguracaoTelemetria(this IConfiguration configuration) =>
        configuration.GetSection(ConfiguracaoTelemetria.Secao).Get<ConfiguracaoTelemetria>() ?? new ConfiguracaoTelemetria();

    public static WebApplicationBuilder AdicionarTelemetria(this WebApplicationBuilder builder, string nomeServico)
    {
        var configuracao = builder.Configuration.ObterConfiguracaoTelemetria();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(recurso => recurso
                .AddService(serviceName: nomeServico, serviceNamespace: "concessionaria", serviceVersion: "1.0.0")
                .AddAttributes([new KeyValuePair<string, object>("deployment.environment.name", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(Telemetria.Nome)
                    .AddSource("MongoDB.Driver")
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
                // Retries, timeouts e circuito aberto do AddStandardResilienceHandler.
                .AddMeter("Polly")
                .AddPrometheusExporter());

        return builder;
    }

    public static WebApplication MapearMetricas(this WebApplication app)
    {
        app.MapPrometheusScrapingEndpoint(RotaMetricas);
        return app;
    }
}
