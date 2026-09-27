using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.SystemConsole.Themes;

namespace Concessionaria.Bff.Comum.Observabilidade;

public static class ExtensoesObservabilidade
{
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
            // Logs também vão para o Collector (e dali para o Loki) com TraceId/SpanId, ligando log e trace no Grafana.
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

    // Toda resposta de erro (ProblemDetails) carrega o Id de operação, para o front/usuário informar ao suporte.
    public static IServiceCollection AdicionarProblemDetails(this IServiceCollection services) =>
        services.AddProblemDetails(opcoes => opcoes.CustomizeProblemDetails = contexto =>
        {
            if (ContextoOperacao.IdAtual is { } idOperacao)
                contexto.ProblemDetails.Extensions["idOperacao"] = idOperacao;
        });

    // Ordem importa: o Id de operação vem antes de tudo para que até o log da requisição e os erros já saiam correlacionados.
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
