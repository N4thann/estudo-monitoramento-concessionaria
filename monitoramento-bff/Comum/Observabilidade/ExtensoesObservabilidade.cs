using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;

namespace Concessionaria.Bff.Comum.Observabilidade;

/// <summary>Logs (Serilog), ProblemDetails com Id de operação e o início do pipeline HTTP.</summary>
public static class ExtensoesObservabilidade
{
    /// <summary>
    /// Configura o Serilog com três destinos: console, arquivo diário em <c>logs/</c> (7 dias) e OTLP para o
    /// Collector (e dali para o Loki), com TraceId/SpanId ligando log e trace no Grafana.
    /// </summary>
    public static WebApplicationBuilder AdicionarSerilog(this WebApplicationBuilder builder, string nomeAplicacao)
    {
        var endpointOtlp = builder.Configuration.ObterConfiguracaoTelemetria().EndpointOtlp;

        builder.Services.AddSerilog((services, configuracao) => configuracao
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Aplicacao", nomeAplicacao)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Aplicacao}] [{IdOperacao}] {Message:lj}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code)
            .WriteTo.File(
                path: $"logs/{nomeAplicacao}-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{IdOperacao}] [{TraceId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
            .WriteTo.OpenTelemetry(opcoes =>
            {
                opcoes.Endpoint = endpointOtlp;
                opcoes.Protocol = OtlpProtocol.Grpc;
                opcoes.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = nomeAplicacao,
                    ["service.namespace"] = "concessionaria"
                };
            }));

        return builder;
    }

    /// <summary>Toda resposta de erro (ProblemDetails) carrega o Id de operação, para o front/usuário informar ao suporte.</summary>
    public static IServiceCollection AdicionarProblemDetails(this IServiceCollection services) =>
        services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
        {
            if (ContextoOperacao.IdAtual is { } idOperacao)
                contexto.ProblemDetails.Extensions["idOperacao"] = idOperacao;
        });

    /// <summary>
    /// Primeiros middlewares do pipeline, nesta ordem: Id de operação e log de requisição. O Id vem antes de tudo
    /// para que até o log da requisição e os erros já saiam correlacionados.
    /// </summary>
    public static WebApplication UsarObservabilidade(this WebApplication app)
    {
        app.UseMiddleware<MiddlewareIdOperacao>();
        app.UseSerilogRequestLogging(opcoes =>
        {
            opcoes.MessageTemplate = "HTTP {RequestMethod} {RequestPath} respondeu {StatusCode} em {Elapsed:0.0} ms";
            opcoes.GetLevel = (contexto, _, excecao) =>
                excecao is not null || contexto.Response.StatusCode >= 500 ? LogEventLevel.Error
                : ExtensoesTelemetria.EhRotaInfraestrutura(contexto.Request.Path) ? LogEventLevel.Verbose
                : LogEventLevel.Information;
        });

        return app;
    }
}
