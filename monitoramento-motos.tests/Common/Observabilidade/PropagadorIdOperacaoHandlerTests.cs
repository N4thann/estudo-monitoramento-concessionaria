using Concessionaria.Motos.Comum.Observabilidade;
using Concessionaria.Motos.tests.Fakes.Http;
using Shouldly;

namespace Concessionaria.Motos.tests.Common.Observabilidade
{
    /// <summary>Repasse do Id de operação nas chamadas de saída para Relatórios.</summary>
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
            ContextoOperacao.IdAtual = "id-vindo-do-bff";
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "http://relatorios/relatorios/indicadores/vendas");

            // Act
            await _sut.SendAsync(requisicao, CancellationToken.None);

            // Assert
            _destino.Recebida!.Headers.GetValues(ContextoOperacao.Cabecalho).ShouldBe(["id-vindo-do-bff"]);
        }

        [Fact(DisplayName = "SUCESSO - Não deve sobrescrever um Id de operação já definido na requisição")]
        public async Task SendAsync_WhenRequestAlreadyHasHeader_ShouldKeepIt()
        {
            // Arrange
            ContextoOperacao.IdAtual = "id-vindo-do-bff";
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "http://relatorios/");
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
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "http://relatorios/");

            // Act
            await _sut.SendAsync(requisicao, CancellationToken.None);

            // Assert
            _destino.Recebida!.Headers.Contains(ContextoOperacao.Cabecalho).ShouldBeFalse();
        }
    }
}
