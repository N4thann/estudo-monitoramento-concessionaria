using Microsoft.AspNetCore.Diagnostics;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Refit;

namespace Concessionaria.Motos.Comum;

/// <summary>
/// Falha de comunicação com outro serviço vira 502/503/504 (e não 500 genérico), para diferenciar
/// "esta API quebrou" de "um serviço que ela chama está fora" nos logs e nas métricas.
/// </summary>
public class ManipuladorFalhaIntegracao(IProblemDetailsService problemDetailsService, ILogger<ManipuladorFalhaIntegracao> logger) : IExceptionHandler
{
    /// <summary>
    /// Circuito aberto vira 503, timeout 504 e falha de conexão 502. Qualquer outra exceção não é tratada aqui
    /// (devolve falso) e segue para o tratamento padrão (500).
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // O Refit embrulha falhas de rede/resiliência em ApiRequestException; a causa real fica em InnerException.
        var causa = exception is ApiRequestException { InnerException: { } interna } ? interna : exception;

        var (statusCode, titulo) = causa switch
        {
            BrokenCircuitException => (StatusCodes.Status503ServiceUnavailable, "Serviço dependente temporariamente indisponível."),
            TimeoutRejectedException => (StatusCodes.Status504GatewayTimeout, "Serviço dependente não respondeu a tempo."),
            HttpRequestException => (StatusCodes.Status502BadGateway, "Falha ao comunicar com um serviço dependente."),
            _ => (0, string.Empty)
        };

        if (statusCode == 0)
            return false;

        logger.LogError(exception, "Falha de integração durante {Metodo} {Caminho}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = statusCode, Title = titulo }
        });
    }
}
