using Concessionaria.Motos.Comum;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;

namespace Concessionaria.Motos.tests.Common
{
    /// <summary>Conversão dos erros de negócio (<see cref="ErrorOr"/>) em respostas ProblemDetails.</summary>
    public class ExtensoesErrorOrTests
    {
        [Fact(DisplayName = "ERRO - Deve devolver 400 com os erros agrupados por campo quando todos são de validação")]
        public void ParaRespostaHttp_WhenAllErrorsAreValidation_ShouldReturnValidationProblemGroupedByField()
        {
            // Arrange
            List<Error> erros =
            [
                Error.Validation("Marca", "Marca é obrigatória."),
                Error.Validation("Marca", "Marca muito longa."),
                Error.Validation("Preco", "Preço deve ser maior que zero.")
            ];

            // Act
            var resultado = (ProblemHttpResult)erros.ParaRespostaHttp();

            // Assert
            resultado.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
            resultado.ProblemDetails.Title.ShouldBe("Um ou mais campos são inválidos.");
            var validacao = resultado.ProblemDetails.ShouldBeOfType<HttpValidationProblemDetails>();
            validacao.Errors["Marca"].ShouldBe(["Marca é obrigatória.", "Marca muito longa."]);
            validacao.Errors["Preco"].ShouldBe(["Preço deve ser maior que zero."]);
        }

        [Theory(DisplayName = "ERRO - Deve mapear o tipo do erro de negócio para o status HTTP, com o código no ProblemDetails")]
        [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
        [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
        [InlineData(ErrorType.Failure, StatusCodes.Status502BadGateway)]
        [InlineData(ErrorType.Unexpected, StatusCodes.Status500InternalServerError)]
        public void ParaRespostaHttp_WhenErrorIsBusinessError_ShouldMapTypeToStatusCode(ErrorType tipo, int statusEsperado)
        {
            // Arrange
            List<Error> erros = [Error.Custom((int)tipo, "Moto.Codigo", "Descrição do erro.")];

            // Act
            var resultado = (ProblemHttpResult)erros.ParaRespostaHttp();

            // Assert
            resultado.StatusCode.ShouldBe(statusEsperado);
            resultado.ProblemDetails.Title.ShouldBe("Descrição do erro.");
            resultado.ProblemDetails.Extensions["codigo"].ShouldBe("Moto.Codigo");
        }

        [Fact(DisplayName = "ERRO - Deve usar o primeiro erro quando validação e erro de negócio vêm misturados")]
        public void ParaRespostaHttp_WhenErrorsAreMixed_ShouldUseFirstError()
        {
            // Arrange
            List<Error> erros = [Error.Conflict("Moto.JaVendido", "Moto já foi vendida."), Error.Validation("Preco", "Inválido.")];

            // Act
            var resultado = (ProblemHttpResult)erros.ParaRespostaHttp();

            // Assert
            resultado.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
            resultado.ProblemDetails.ShouldNotBeOfType<HttpValidationProblemDetails>();
        }
    }
}
