using Concessionaria.Bff.Comum;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Refit;
using Shouldly;

namespace Concessionaria.Bff.tests.Common
{
    /// <summary>Falha de comunicação com outro serviço vira 502/503/504, e não 500 genérico.</summary>
    public class ManipuladorFalhaIntegracaoTests
    {
        private readonly IProblemDetailsService _problemDetailsMock;
        private readonly ILogger<ManipuladorFalhaIntegracao> _loggerMock;
        private readonly DefaultHttpContext _contexto;
        private ProblemDetailsContext? _escrito;
        private readonly ManipuladorFalhaIntegracao _sut;

        /// <summary>Monta o manipulador com um serviço de ProblemDetails que captura o que seria escrito.</summary>
        public ManipuladorFalhaIntegracaoTests()
        {
            _problemDetailsMock = Substitute.For<IProblemDetailsService>();
            _problemDetailsMock.TryWriteAsync(Arg.Do<ProblemDetailsContext>(c => _escrito = c)).Returns(new ValueTask<bool>(true));
            _loggerMock = Substitute.For<ILogger<ManipuladorFalhaIntegracao>>();
            _contexto = new DefaultHttpContext();
            _sut = new ManipuladorFalhaIntegracao(_problemDetailsMock, _loggerMock);
        }

        /// <summary>Embrulha a causa como o Refit faz numa falha de rede ou de resiliência.</summary>
        private static ApiRequestException EmbrulhadaPeloRefit(Exception causa) =>
            new(new HttpRequestMessage(HttpMethod.Post, "http://carros/"), HttpMethod.Post, new RefitSettings(), causa);

        [Fact(DisplayName = "ERRO - Deve devolver 503 quando o circuito para o serviço dependente está aberto")]
        public async Task TryHandleAsync_WhenCircuitIsOpen_ShouldReturnServiceUnavailable()
        {
            // Act
            var tratado = await _sut.TryHandleAsync(_contexto, new BrokenCircuitException(), CancellationToken.None);

            // Assert
            tratado.ShouldBeTrue();
            _contexto.Response.StatusCode.ShouldBe(StatusCodes.Status503ServiceUnavailable);
            _escrito!.ProblemDetails.Status.ShouldBe(StatusCodes.Status503ServiceUnavailable);
            _escrito.ProblemDetails.Title.ShouldBe("Serviço dependente temporariamente indisponível.");
        }

        [Fact(DisplayName = "ERRO - Deve devolver 504 quando o serviço dependente estoura o timeout")]
        public async Task TryHandleAsync_WhenDependencyTimesOut_ShouldReturnGatewayTimeout()
        {
            // Act
            var tratado = await _sut.TryHandleAsync(_contexto, new TimeoutRejectedException(), CancellationToken.None);

            // Assert
            tratado.ShouldBeTrue();
            _contexto.Response.StatusCode.ShouldBe(StatusCodes.Status504GatewayTimeout);
            _escrito!.ProblemDetails.Title.ShouldBe("Serviço dependente não respondeu a tempo.");
        }

        [Fact(DisplayName = "ERRO - Deve devolver 502 quando a conexão com o serviço dependente falha")]
        public async Task TryHandleAsync_WhenConnectionFails_ShouldReturnBadGateway()
        {
            // Act
            var tratado = await _sut.TryHandleAsync(_contexto, new HttpRequestException("Conexão recusada"), CancellationToken.None);

            // Assert
            tratado.ShouldBeTrue();
            _contexto.Response.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
            _escrito!.ProblemDetails.Title.ShouldBe("Falha ao comunicar com um serviço dependente.");
        }

        [Fact(DisplayName = "ERRO - Deve usar a causa real quando o Refit embrulha a falha em ApiRequestException")]
        public async Task TryHandleAsync_WhenRefitWrapsTimeout_ShouldUseInnerExceptionToChooseStatus()
        {
            // Arrange
            var excecao = EmbrulhadaPeloRefit(new TimeoutRejectedException());

            // Act
            var tratado = await _sut.TryHandleAsync(_contexto, excecao, CancellationToken.None);

            // Assert
            tratado.ShouldBeTrue();
            _contexto.Response.StatusCode.ShouldBe(StatusCodes.Status504GatewayTimeout);
            _escrito!.Exception.ShouldBeSameAs(excecao);
        }

        [Fact(DisplayName = "ERRO - Não deve tratar exceção que não é de integração, deixando o 500 padrão")]
        public async Task TryHandleAsync_WhenExceptionIsNotIntegrationFailure_ShouldNotHandle()
        {
            // Act
            var tratado = await _sut.TryHandleAsync(_contexto, new InvalidOperationException("Bug local"), CancellationToken.None);

            // Assert
            tratado.ShouldBeFalse();
            _contexto.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
            await _problemDetailsMock.DidNotReceiveWithAnyArgs().TryWriteAsync(default!);
        }
    }
}
