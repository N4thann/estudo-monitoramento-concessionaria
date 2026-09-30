using Concessionaria.Relatorios.Comum.Observabilidade;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Common.Observabilidade
{
    public class ExtensoesObservabilidadeTests
    {
        private readonly ProblemDetailsOptions _opcoes;

        public ExtensoesObservabilidadeTests()
        {
            using var provedor = new ServiceCollection().AdicionarProblemDetails().BuildServiceProvider();
            _opcoes = provedor.GetRequiredService<IOptions<ProblemDetailsOptions>>().Value;
        }

        private ProblemDetails Customizar()
        {
            var contexto = new ProblemDetailsContext { HttpContext = new DefaultHttpContext(), ProblemDetails = new ProblemDetails() };
            _opcoes.CustomizeProblemDetails!(contexto);
            return contexto.ProblemDetails;
        }

        [Fact(DisplayName = "SUCESSO - Deve incluir o Id de operação atual em todo ProblemDetails")]
        public void AdicionarProblemDetails_WhenThereIsCurrentOperationId_ShouldAddItToExtensions()
        {
            // Arrange
            ContextoOperacao.IdAtual = "id-da-requisicao";

            // Act
            var problema = Customizar();

            // Assert
            problema.Extensions["idOperacao"].ShouldBe("id-da-requisicao");
        }

        [Fact(DisplayName = "SUCESSO - Não deve incluir Id de operação quando a execução não tem um")]
        public void AdicionarProblemDetails_WhenThereIsNoCurrentOperationId_ShouldNotAddExtension()
        {
            // Arrange
            ContextoOperacao.IdAtual = null;

            // Act
            var problema = Customizar();

            // Assert
            problema.Extensions.ContainsKey("idOperacao").ShouldBeFalse();
        }
    }
}
