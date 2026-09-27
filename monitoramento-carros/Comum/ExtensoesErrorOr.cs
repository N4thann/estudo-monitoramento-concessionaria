using ErrorOr;

namespace Concessionaria.Carros.Comum;

public static class ExtensoesErrorOr
{
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
