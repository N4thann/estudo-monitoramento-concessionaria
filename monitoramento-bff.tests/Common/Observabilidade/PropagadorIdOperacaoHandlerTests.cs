using Concessionaria.Bff.Comum.Observabilidade;
using Concessionaria.Bff.tests.Fakes.Http;
using Shouldly;

namespace Concessionaria.Bff.tests.Common.Observabilidade
{
    /// <summary>Repasse do Id de operação nas chamadas de saída para Carros e Motos.</summary>
    public class PropagadorIdOperacaoHandlerTests
    {
        private readonly HandlerCapturaRequisicao _destino;
        private readonly HttpMessageInvoker _sut;

        /// <summary>Encadeia o propagador a um destino que só captura a requisição.</summary>
        public PropagadorIdOperacaoHandlerTests()
        {
            _destino = new HandlerCapturaRequisicao();
            _sut = new HttpMessageInvoker(new PropagadorIdOperacaoHandler { InnerHandler = _destino });
        }

        [Fact(DisplayName = "SUCESSO - Deve repassar o Id de operação atual no cabeçalho da chamada de saída")]
        public async Task SendAsync_WhenThereIsCurrentOperationId_ShouldAddHeader()
        {
            // Arrange
            ContextoOperacao.IdAtual = "id-vindo-do-front";
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "http://carros/carros/resumo");

            // Act
            await _sut.SendAsync(requisicao, CancellationToken.None);

            // Assert
            _destino.Recebida!.Headers.GetValues(ContextoOperacao.Cabecalho).ShouldBe(["id-vindo-do-front"]);
        }

        [Fact(DisplayName = "SUCESSO - Não deve sobrescrever um Id de operação já definido na requisição")]
        public async Task SendAsync_WhenRequestAlreadyHasHeader_ShouldKeepIt()
        {
            // Arrange
            ContextoOperacao.IdAtual = "id-vindo-do-front";
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "http://carros/");
            requisicao.Headers.Add(ContextoOperacao.Cabecalho, "id-explicito");

            // Act
            await _sut.SendAsync(requisicao, CancellationToken.None);

            // Assert
            _destino.Recebida!.Headers.GetValues(ContextoOperacao.Cabecalho).ShouldBe(["id-explicito"]);
        }

        [Fact(DisplayName = "SUCESSO - Não deve incluir o cabeçalho quando a execução não tem Id de operação")]
        public async Task SendAsync_WhenThereIsNoCurrentOperationId_ShouldNotAddHeader()
        {
            // Arrange
            ContextoOperacao.IdAtual = null;
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "http://carros/");

            // Act
            await _sut.SendAsync(requisicao, CancellationToken.None);

            // Assert
            _destino.Recebida!.Headers.Contains(ContextoOperacao.Cabecalho).ShouldBeFalse();
        }
    }
}
