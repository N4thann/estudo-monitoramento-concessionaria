using System.Diagnostics;
using Concessionaria.Carros.Comum.Observabilidade;
using Concessionaria.Carros.tests.Fakes.Http;
using Concessionaria.Carros.tests.Fakes.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Serilog;
using Serilog.Events;
using Shouldly;

namespace Concessionaria.Carros.tests.Common.Observabilidade
{
    /// <summary>Id de operação na borda: reaproveitado ou gerado, devolvido na resposta, no span e nos logs.</summary>
    public class MiddlewareIdOperacaoTests
    {
        private readonly RespostaHttpFalsa _resposta;
        private readonly DefaultHttpContext _contexto;
        private string? _idVistoPeloProximo;
        private int _chamadasAoProximo;
        private readonly MiddlewareIdOperacao _sut;

        /// <summary>Monta o middleware com um próximo passo que registra o Id visto.</summary>
        public MiddlewareIdOperacaoTests()
        {
            _resposta = new RespostaHttpFalsa();
            _contexto = new DefaultHttpContext();
            _contexto.Features.Set<IHttpResponseFeature>(_resposta);
            _sut = new MiddlewareIdOperacao(_ =>
            {
                _chamadasAoProximo++;
                _idVistoPeloProximo = ContextoOperacao.IdAtual;
                return Task.CompletedTask;
            });
        }

        [Fact(DisplayName = "SUCESSO - Deve reaproveitar o Id de operação recebido do BFF no cabeçalho")]
        public async Task InvokeAsync_WhenHeaderHasValidId_ShouldReuseIt()
        {
            // Arrange
            _contexto.Request.Headers[ContextoOperacao.Cabecalho] = "id-vindo-do-bff";

            // Act
            await _sut.InvokeAsync(_contexto);

            // Assert
            _idVistoPeloProximo.ShouldBe("id-vindo-do-bff");
            _chamadasAoProximo.ShouldBe(1);
        }

        [Fact(DisplayName = "SUCESSO - Deve aceitar Id de operação com exatamente 64 caracteres")]
        public async Task InvokeAsync_WhenHeaderIdHasMaximumLength_ShouldReuseIt()
        {
            // Arrange
            var id = new string('a', 64);
            _contexto.Request.Headers[ContextoOperacao.Cabecalho] = id;

            // Act
            await _sut.InvokeAsync(_contexto);

            // Assert
            _idVistoPeloProximo.ShouldBe(id);
        }

        [Theory(DisplayName = "SUCESSO - Deve gerar um novo Id de operação quando o cabeçalho está ausente ou em branco")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task InvokeAsync_WhenHeaderIsMissingOrBlank_ShouldGenerateNewId(string? cabecalho)
        {
            // Arrange
            if (cabecalho is not null)
                _contexto.Request.Headers[ContextoOperacao.Cabecalho] = cabecalho;

            // Act
            await _sut.InvokeAsync(_contexto);

            // Assert
            _idVistoPeloProximo.ShouldNotBeNull();
            _idVistoPeloProximo.ShouldMatch("^[0-9a-f]{32}$");
        }

        [Fact(DisplayName = "SUCESSO - Deve descartar Id de operação maior que 64 caracteres e gerar outro")]
        public async Task InvokeAsync_WhenHeaderIdIsTooLong_ShouldGenerateNewId()
        {
            // Arrange
            _contexto.Request.Headers[ContextoOperacao.Cabecalho] = new string('a', 65);

            // Act
            await _sut.InvokeAsync(_contexto);

            // Assert
            _idVistoPeloProximo!.ShouldMatch("^[0-9a-f]{32}$");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver o Id de operação no cabeçalho da resposta")]
        public async Task InvokeAsync_WhenResponseStarts_ShouldWriteIdOnResponseHeader()
        {
            // Arrange
            _contexto.Request.Headers[ContextoOperacao.Cabecalho] = "id-vindo-do-bff";

            // Act
            await _sut.InvokeAsync(_contexto);
            await _resposta.IniciarAsync();

            // Assert
            _contexto.Response.Headers[ContextoOperacao.Cabecalho].ToString().ShouldBe("id-vindo-do-bff");
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver na resposta o mesmo Id gerado quando o cabeçalho não veio")]
        public async Task InvokeAsync_WhenIdIsGenerated_ShouldWriteSameIdOnResponseHeader()
        {
            // Act
            await _sut.InvokeAsync(_contexto);
            await _resposta.IniciarAsync();

            // Assert
            _contexto.Response.Headers[ContextoOperacao.Cabecalho].ToString().ShouldBe(_idVistoPeloProximo);
        }

        [Fact(DisplayName = "SUCESSO - Deve marcar o span atual com o Id de operação")]
        public async Task InvokeAsync_WhenThereIsCurrentActivity_ShouldTagItWithOperationId()
        {
            // Arrange
            _contexto.Request.Headers[ContextoOperacao.Cabecalho] = "id-vindo-do-bff";
            using var span = new Activity("requisicao-teste").Start();

            // Act
            await _sut.InvokeAsync(_contexto);

            // Assert
            span.GetTagItem("concessionaria.id_operacao").ShouldBe("id-vindo-do-bff");
        }

        [Fact(DisplayName = "SUCESSO - Deve incluir o Id de operação nos logs escritos durante a requisição e remover depois")]
        public async Task InvokeAsync_WhenLoggingDuringRequest_ShouldEnrichLogsWithOperationId()
        {
            // Arrange
            var coletor = new ColetorLogs();
            using var logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Sink(coletor).CreateLogger();
            var sut = new MiddlewareIdOperacao(_ =>
            {
                logger.Information("Dentro da requisição");
                return Task.CompletedTask;
            });
            _contexto.Request.Headers[ContextoOperacao.Cabecalho] = "id-vindo-do-bff";

            // Act
            await sut.InvokeAsync(_contexto);
            logger.Information("Fora da requisição");

            // Assert
            coletor.Eventos[0].Properties[ContextoOperacao.PropriedadeLog].ShouldBe(new ScalarValue("id-vindo-do-bff"));
            coletor.Eventos[1].Properties.ContainsKey(ContextoOperacao.PropriedadeLog).ShouldBeFalse();
        }
    }
}
