using ErrorOr;

namespace Concessionaria.Carros.Comum;

/// <summary>Conversão dos erros de negócio (<see cref="Error"/>) em resposta HTTP com ProblemDetails.</summary>
public static class ExtensoesErrorOr
{
    /// <summary>
    /// Só erros de validação viram 400 com os erros agrupados por campo (<c>errors</c>). Nos demais casos vale o
    /// primeiro erro: NotFound 404, Conflict 409, Failure 502 e qualquer outro 500, com o código em <c>codigo</c>.
    /// </summary>
    public static IResult ParaRespostaHttp(this List<Error> erros)
    {
        if (erros.All(e => e.Type == ErrorType.Validation))
        {
            var errosPorCampo = erros
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());

            return Results.ValidationProblem(errosPorCampo, title: "Um ou mais campos são inválidos.");
        }

        var erro = erros[0];
        var statusCode = erro.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            // Neste projeto, Failure representa falha de um serviço dependente (ex.: Relatórios).
            ErrorType.Failure => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            statusCode: statusCode,
            title: erro.Description,
            extensions: new Dictionary<string, object?> { ["codigo"] = erro.Code });
    }
}
