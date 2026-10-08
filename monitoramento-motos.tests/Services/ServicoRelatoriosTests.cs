using System.Net;
using System.Net.Http.Headers;
using Concessionaria.Motos.Aplicacao.Comum;
using Concessionaria.Motos.Aplicacao.Features.Relatorios;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Enums;
using Concessionaria.Motos.Dominio.Interfaces;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;
using Concessionaria.Motos.tests.Common.Refit;
using Concessionaria.Motos.tests.DataBuilder;
using ErrorOr;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly.Timeout;
using Shouldly;

namespace Concessionaria.Motos.tests.Services
{
    /// <summary>Lado de Motos da integração com Relatórios: período, dados enviados e leitura da planilha.</summary>
    public class ServicoRelatoriosTests
    {
        private static readonly byte[] Planilha = [0x50, 0x4B, 0x03, 0x04];

        private readonly IRepositorioMotos _repositorioMotosMock;
        private readonly IRepositorioVendas _repositorioVendasMock;
        private readonly IRelatoriosApi _relatoriosApiMock;
        private readonly ServicoRelatorios _sut;

        /// <summary>Monta o serviço com repositórios e cliente de Relatórios substituídos; vendas e estoque começam vazios.</summary>
        public ServicoRelatoriosTests()
        {
            _repositorioMotosMock = Substitute.For<IRepositorioMotos>();
            _repositorioVendasMock = Substitute.For<IRepositorioVendas>();
            _relatoriosApiMock = Substitute.For<IRelatoriosApi>();
            _repositorioVendasMock.ListarPorPeriodoAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(new List<Venda>());
            _repositorioMotosMock.ListarPorSituacaoAsync(Arg.Any<SituacaoVeiculo>(), Arg.Any<CancellationToken>()).Returns(new List<Moto>());
            _sut = new ServicoRelatorios(_repositorioMotosMock, _repositorioVendasMock, _relatoriosApiMock, Substitute.For<ILogger<ServicoRelatorios>>());
        }

        /// <summary>Faz Relatórios devolver a planilha de vendas com o cabeçalho informado.</summary>
        private void PlanilhaVendasDevolvida(ContentDispositionHeaderValue? disposicao = null) =>
            _relatoriosApiMock.GerarPlanilhaVendasAsync(Arg.Any<SolicitacaoPlanilhaVendas>(), Arg.Any<CancellationToken>())
                .Returns(_ => RespostasApi.Arquivo(Planilha, disposicao));

        [Fact(DisplayName = "SUCESSO - Deve devolver os indicadores calculados por Relatórios a partir das vendas")]
        public async Task CalcularIndicadoresAsync_WhenReportsServiceResponds_ShouldReturnIndicators()
        {
            // Arrange
            var venda = VendaDataBuilder.Create().WithDescricaoVeiculo("Fiat Uno 2020").WithMarca("Fiat").WithNomeCliente("Ana").WithDocumentoCliente("12345678909").WithValor(30_000m).Build();
            var indicadores = new IndicadoresVendas(1, 30_000m, 30_000m, 30_000m, "Fiat");
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Sucesso(indicadores));

            // Act
            var resultado = await _sut.CalcularIndicadoresAsync([venda], CancellationToken.None);

            // Assert
            resultado.ShouldBe(indicadores);
            await _relatoriosApiMock.Received(1).CalcularIndicadoresVendasAsync(
                Arg.Is<SolicitacaoIndicadoresVendas>(s => s.Vendas.Single() == new VendaRelatorio("Fiat Uno 2020", "Fiat", "Ana", "12345678909", 30_000m, venda.VendidaEm)),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver nulo, sem lançar exceção, quando Relatórios não responde a tempo")]
        public async Task CalcularIndicadoresAsync_WhenReportsServiceTimesOut_ShouldReturnNull()
        {
            // Arrange
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<IndicadoresVendas>(new TimeoutRejectedException()));

            // Act
            var resultado = await _sut.CalcularIndicadoresAsync([], CancellationToken.None);

            // Assert
            resultado.ShouldBeNull();
        }

        [Fact(DisplayName = "SUCESSO - Deve pedir a planilha dos últimos 30 dias quando o período não é informado")]
        public async Task GerarPlanilhaVendasAsync_WhenPeriodIsMissing_ShouldUseLast30Days()
        {
            // Arrange
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            var inicio = hoje.AddDays(-30);
            PlanilhaVendasDevolvida();

            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(null, null), CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            await _repositorioVendasMock.Received(1).ListarPorPeriodoAsync(
                inicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                hoje.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Arg.Any<CancellationToken>());
            await _relatoriosApiMock.Received(1).GerarPlanilhaVendasAsync(
                Arg.Is<SolicitacaoPlanilhaVendas>(s => s.TipoVeiculo == "Motos" && s.De == inicio && s.Ate == hoje),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "SUCESSO - Deve enviar as vendas do período informado e devolver o arquivo com o nome vindo de Relatórios")]
        public async Task GerarPlanilhaVendasAsync_WhenPeriodIsValid_ShouldReturnFileWithNameFromReports()
        {
            // Arrange
            var vendas = VendaDataBuilder.AsList(3);
            _repositorioVendasMock.ListarPorPeriodoAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(vendas);
            PlanilhaVendasDevolvida(new ContentDispositionHeaderValue("attachment") { FileName = "\"relatorio-vendas-motos_20260901_20260930.xlsx\"" });

            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)), CancellationToken.None);

            // Assert
            resultado.Value.NomeArquivo.ShouldBe("relatorio-vendas-motos_20260901_20260930.xlsx");
            resultado.Value.Conteudo.ShouldBe(Planilha);
            await _relatoriosApiMock.Received(1).GerarPlanilhaVendasAsync(
                Arg.Is<SolicitacaoPlanilhaVendas>(s => s.Vendas.Count == 3 && s.De == new DateOnly(2026, 9, 1) && s.Ate == new DateOnly(2026, 9, 30)),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "SUCESSO - Deve preferir o nome de arquivo codificado (filename*) quando Relatórios envia os dois")]
        public async Task GerarPlanilhaVendasAsync_WhenFileNameStarIsPresent_ShouldUseIt()
        {
            // Arrange
            PlanilhaVendasDevolvida(new ContentDispositionHeaderValue("attachment") { FileName = "simples.xlsx", FileNameStar = "relatório-çã.xlsx" });

            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(null, null), CancellationToken.None);

            // Assert
            resultado.Value.NomeArquivo.ShouldBe("relatório-çã.xlsx");
        }

        [Fact(DisplayName = "SUCESSO - Deve usar um nome padrão quando Relatórios não envia o nome do arquivo")]
        public async Task GerarPlanilhaVendasAsync_WhenContentDispositionIsMissing_ShouldUseDefaultName()
        {
            // Arrange
            PlanilhaVendasDevolvida();

            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(null, null), CancellationToken.None);

            // Assert
            resultado.Value.NomeArquivo.ShouldBe("relatorio-vendas.xlsx");
        }

        [Fact(DisplayName = "SUCESSO - Deve aceitar período de exatamente 366 dias")]
        public async Task GerarPlanilhaVendasAsync_WhenPeriodHasMaximumLength_ShouldGenerate()
        {
            // Arrange
            PlanilhaVendasDevolvida();
            var de = new DateOnly(2025, 1, 1);

            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(de, de.AddDays(366)), CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
        }

        [Fact(DisplayName = "ERRO - Deve recusar período maior que 366 dias sem consultar o banco nem Relatórios")]
        public async Task GerarPlanilhaVendasAsync_WhenPeriodIsTooLong_ShouldReturnValidationErrorWithoutCalls()
        {
            // Arrange
            var de = new DateOnly(2025, 1, 1);

            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(de, de.AddDays(367)), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.Validation);
            resultado.FirstError.Code.ShouldBe("Periodo");
            await _repositorioVendasMock.DidNotReceiveWithAnyArgs().ListarPorPeriodoAsync(default, default, default);
            await _relatoriosApiMock.DidNotReceiveWithAnyArgs().GerarPlanilhaVendasAsync(default!, default);
        }

        [Fact(DisplayName = "ERRO - Deve recusar período com data inicial posterior à final")]
        public async Task GerarPlanilhaVendasAsync_WhenStartIsAfterEnd_ShouldReturnValidationError()
        {
            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 1)), CancellationToken.None);

            // Assert
            resultado.FirstError.Code.ShouldBe("Periodo");
        }

        [Fact(DisplayName = "SUCESSO - Deve começar 30 dias antes da data final quando só ela é informada")]
        public async Task GerarPlanilhaVendasAsync_WhenOnlyEndIsInformed_ShouldStart30DaysBefore()
        {
            // Arrange
            PlanilhaVendasDevolvida();

            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(null, new DateOnly(2026, 9, 30)), CancellationToken.None);

            // Assert
            resultado.IsError.ShouldBeFalse();
            await _relatoriosApiMock.Received(1).GerarPlanilhaVendasAsync(
                Arg.Is<SolicitacaoPlanilhaVendas>(s => s.De == new DateOnly(2026, 8, 31) && s.Ate == new DateOnly(2026, 9, 30)),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver falha de geração quando Relatórios responde com erro")]
        public async Task GerarPlanilhaVendasAsync_WhenReportsServiceFails_ShouldReturnFailure()
        {
            // Arrange
            _relatoriosApiMock.GerarPlanilhaVendasAsync(Arg.Any<SolicitacaoPlanilhaVendas>(), Arg.Any<CancellationToken>())
                .Returns(await RespostasApi.ErroHttpAsync<Stream>(HttpStatusCode.InternalServerError, "{\"title\":\"erro\"}"));

            // Act
            var resultado = await _sut.GerarPlanilhaVendasAsync(new FiltroPeriodo(null, null), CancellationToken.None);

            // Assert
            resultado.FirstError.Type.ShouldBe(ErrorType.Failure);
            resultado.FirstError.Code.ShouldBe("Relatorios.FalhaNaGeracao");
        }

        [Fact(DisplayName = "SUCESSO - Deve enviar só as motos disponíveis, com a cilindrada como detalhe do estoque")]
        public async Task GerarPlanilhaEstoqueAsync_WhenThereAreAvailableCars_ShouldSendThemWithDisplacementAsDetail()
        {
            // Arrange
            var moto = MotoDataBuilder.Create().WithMarca("Fiat").WithModelo("Uno").WithAno(2020).WithCor("Branco").WithCilindrada(160).WithPreco(40_000m).Build();
            _repositorioMotosMock.ListarPorSituacaoAsync(SituacaoVeiculo.Disponivel, Arg.Any<CancellationToken>()).Returns(new List<Moto> { moto });
            _relatoriosApiMock.GerarPlanilhaEstoqueAsync(Arg.Any<SolicitacaoPlanilhaEstoque>(), Arg.Any<CancellationToken>()).Returns(RespostasApi.Arquivo(Planilha));

            // Act
            var resultado = await _sut.GerarPlanilhaEstoqueAsync(CancellationToken.None);

            // Assert
            resultado.Value.NomeArquivo.ShouldBe("relatorio-estoque.xlsx");
            await _relatoriosApiMock.Received(1).GerarPlanilhaEstoqueAsync(
                Arg.Is<SolicitacaoPlanilhaEstoque>(s => s.TipoVeiculo == "Motos" && s.RotuloDetalhe == "Cilindrada"
                    && s.Veiculos.Single() == new VeiculoRelatorio("Fiat", "Uno", 2020, "Branco", "160", 40_000m, moto.CriadoEm)),
                Arg.Any<CancellationToken>());
        }

        [Fact(DisplayName = "ERRO - Deve devolver falha de geração quando Relatórios está fora")]
        public async Task GerarPlanilhaEstoqueAsync_WhenReportsServiceIsDown_ShouldReturnFailure()
        {
            // Arrange
            _relatoriosApiMock.GerarPlanilhaEstoqueAsync(Arg.Any<SolicitacaoPlanilhaEstoque>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<Stream>(new HttpRequestException("Conexão recusada")));

            // Act
            var resultado = await _sut.GerarPlanilhaEstoqueAsync(CancellationToken.None);

            // Assert
            resultado.FirstError.Code.ShouldBe("Relatorios.FalhaNaGeracao");
        }
    }
}
