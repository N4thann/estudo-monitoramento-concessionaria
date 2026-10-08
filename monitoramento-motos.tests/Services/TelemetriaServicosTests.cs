using System.Diagnostics;
using System.Net;
using Concessionaria.Motos.Aplicacao.Comum;
using Concessionaria.Motos.Aplicacao.Features.Motos;
using Concessionaria.Motos.Aplicacao.Features.Relatorios;
using Concessionaria.Motos.Aplicacao.Features.Vendas;
using Concessionaria.Motos.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Enums;
using Concessionaria.Motos.Dominio.Interfaces;
using Concessionaria.Motos.Dominio.Modelos;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios;
using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;
using Concessionaria.Motos.tests.Common.Observabilidade;
using Concessionaria.Motos.tests.Common.Refit;
using Concessionaria.Motos.tests.DataBuilder;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Motos.tests.Services
{
    /// <summary>Métricas de negócio e spans próprios de Motos que alimentam o dashboard e os alertas.</summary>
    [Collection(ColecaoTelemetria.Nome)]
    public class TelemetriaServicosTests : IDisposable
    {
        private readonly ColetorTelemetria _coletor;
        private readonly IRepositorioMotos _repositorioMotosMock;
        private readonly IRepositorioVendas _repositorioVendasMock;
        private readonly IRelatoriosApi _relatoriosApiMock;
        private readonly ServicoVendas _servicoVendas;
        private readonly ServicoRelatorios _servicoRelatorios;
        private readonly ServicoMotos _servicoMotos;

        /// <summary>Começa a coletar a telemetria e monta os três serviços sobre os mesmos substitutos.</summary>
        public TelemetriaServicosTests()
        {
            _coletor = new ColetorTelemetria();
            _repositorioMotosMock = Substitute.For<IRepositorioMotos>();
            _repositorioVendasMock = Substitute.For<IRepositorioVendas>();
            _relatoriosApiMock = Substitute.For<IRelatoriosApi>();
            _repositorioVendasMock.ListarPorPeriodoAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>()).Returns(new List<Venda>());
            _repositorioMotosMock.ListarPorSituacaoAsync(Arg.Any<SituacaoVeiculo>(), Arg.Any<CancellationToken>()).Returns(new List<Moto>());
            _repositorioMotosMock.ObterTotaisPorSituacaoAsync(Arg.Any<CancellationToken>()).Returns(new Dictionary<SituacaoVeiculo, TotaisEstoque>());

            _servicoVendas = new ServicoVendas(_repositorioMotosMock, _repositorioVendasMock, Substitute.For<ILogger<ServicoVendas>>());
            _servicoRelatorios = new ServicoRelatorios(_repositorioMotosMock, _repositorioVendasMock, _relatoriosApiMock, Substitute.For<ILogger<ServicoRelatorios>>());
            _servicoMotos = new ServicoMotos(_repositorioMotosMock, _repositorioVendasMock, _servicoRelatorios, Substitute.For<ILogger<ServicoMotos>>());
        }

        /// <summary>Para de coletar.</summary>
        public void Dispose() => _coletor.Dispose();

        /// <summary>Configura uma moto existente e diz se a marcação como vendida dá certo.</summary>
        private Moto MotoExistente(bool marcaComoVendido)
        {
            var moto = MotoDataBuilder.Create().Build();
            _repositorioMotosMock.ObterPorIdAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(moto);
            _repositorioMotosMock.TentarMarcarComoVendidoAsync(moto.Id, Arg.Any<CancellationToken>()).Returns(marcaComoVendido);
            return moto;
        }

        /// <summary>Única medição do contador de vendas recusadas, conferindo o tipo de veículo.</summary>
        private MedicaoColetada RecusaUnica()
        {
            var recusa = _coletor.Medicoes("concessionaria.vendas.recusadas").ShouldHaveSingleItem();
            recusa.Tags["tipo_veiculo"].ShouldBe("moto");
            return recusa;
        }

        [Fact(DisplayName = "SUCESSO - Deve contar a venda registrada e somar o valor dela, com tipo de veículo")]
        public async Task RegistrarAsync_WhenSaleIsRegistered_ShouldRecordCountAndValue()
        {
            // Arrange
            var moto = MotoExistente(marcaComoVendido: true);

            // Act
            await _servicoVendas.RegistrarAsync(new RequisicaoVenda(moto.Id, "Maria", "12345678909", 99_900.50m), CancellationToken.None);

            // Assert
            var registrada = _coletor.Medicoes("concessionaria.vendas.registradas").ShouldHaveSingleItem();
            registrada.Valor.ShouldBe(1);
            registrada.Tags["tipo_veiculo"].ShouldBe("moto");
            _coletor.Medicoes("concessionaria.vendas.valor").ShouldHaveSingleItem().Valor.ShouldBe(99_900.50);
            _coletor.Medicoes("concessionaria.vendas.recusadas").ShouldBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve marcar o span da requisição com o Id da venda registrada")]
        public async Task RegistrarAsync_WhenSaleIsRegistered_ShouldTagCurrentSpanWithSaleId()
        {
            // Arrange
            var moto = MotoExistente(marcaComoVendido: true);
            using var span = new Activity("POST /vendas").Start();

            // Act
            var resultado = await _servicoVendas.RegistrarAsync(new RequisicaoVenda(moto.Id, "Maria", "12345678909", 10m), CancellationToken.None);

            // Assert
            span.GetTagItem("concessionaria.venda_id").ShouldBe(resultado.Value.Id);
        }

        [Fact(DisplayName = "ERRO - Deve contar a venda recusada por validação com o motivo 'validacao'")]
        public async Task RegistrarAsync_WhenRequestIsInvalid_ShouldRecordRefusalWithValidationReason()
        {
            // Act
            await _servicoVendas.RegistrarAsync(new RequisicaoVenda("", "", "", 0m), CancellationToken.None);

            // Assert
            RecusaUnica().Tags["motivo"].ShouldBe("validacao");
            _coletor.Medicoes("concessionaria.vendas.registradas").ShouldBeEmpty();
        }

        [Fact(DisplayName = "ERRO - Deve contar a venda recusada de moto inexistente com o motivo 'nao_encontrado'")]
        public async Task RegistrarAsync_WhenCarDoesNotExist_ShouldRecordRefusalWithNotFoundReason()
        {
            // Act
            await _servicoVendas.RegistrarAsync(new RequisicaoVenda(ObjectId.GenerateNewId().ToString(), "Maria", "12345678909", 10m), CancellationToken.None);

            // Assert
            RecusaUnica().Tags["motivo"].ShouldBe("nao_encontrado");
        }

        [Fact(DisplayName = "ERRO - Deve contar a venda recusada de moto já vendida com o motivo 'ja_vendido'")]
        public async Task RegistrarAsync_WhenCarIsAlreadySold_ShouldRecordRefusalWithAlreadySoldReason()
        {
            // Arrange
            var moto = MotoExistente(marcaComoVendido: false);

            // Act
            await _servicoVendas.RegistrarAsync(new RequisicaoVenda(moto.Id, "Maria", "12345678909", 10m), CancellationToken.None);

            // Assert
            RecusaUnica().Tags["motivo"].ShouldBe("ja_vendido");
        }

        [Fact(DisplayName = "ERRO - Deve contar a falha com Relatórios na operação 'indicadores' quando o resumo sai degradado")]
        public async Task CalcularIndicadoresAsync_WhenReportsServiceFails_ShouldRecordIntegrationFailure()
        {
            // Arrange
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<IndicadoresVendas>(new HttpRequestException("Conexão recusada")));

            // Act
            await _servicoRelatorios.CalcularIndicadoresAsync([], CancellationToken.None);

            // Assert
            var falha = _coletor.Medicoes("concessionaria.relatorios.falhas_integracao").ShouldHaveSingleItem();
            falha.Tags["operacao"].ShouldBe("indicadores");
            falha.Tags["tipo_veiculo"].ShouldBe("moto");
        }

        [Fact(DisplayName = "SUCESSO - Não deve contar falha de integração quando Relatórios responde")]
        public async Task CalcularIndicadoresAsync_WhenReportsServiceResponds_ShouldNotRecordFailure()
        {
            // Arrange
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.Sucesso(new IndicadoresVendas(0, 0m, 0m, 0m, null)));

            // Act
            await _servicoRelatorios.CalcularIndicadoresAsync([], CancellationToken.None);

            // Assert
            _coletor.Medicoes("concessionaria.relatorios.falhas_integracao").ShouldBeEmpty();
        }

        [Fact(DisplayName = "ERRO - Deve contar a falha com Relatórios na operação 'planilha_vendas'")]
        public async Task GerarPlanilhaVendasAsync_WhenReportsServiceFails_ShouldRecordIntegrationFailure()
        {
            // Arrange
            _relatoriosApiMock.GerarPlanilhaVendasAsync(Arg.Any<SolicitacaoPlanilhaVendas>(), Arg.Any<CancellationToken>())
                .Returns(await RespostasApi.ErroHttpAsync<Stream>(HttpStatusCode.ServiceUnavailable));

            // Act
            await _servicoRelatorios.GerarPlanilhaVendasAsync(new FiltroPeriodo(null, null), CancellationToken.None);

            // Assert
            _coletor.Medicoes("concessionaria.relatorios.falhas_integracao").ShouldHaveSingleItem().Tags["operacao"].ShouldBe("planilha_vendas");
        }

        [Fact(DisplayName = "ERRO - Deve contar a falha com Relatórios na operação 'planilha_estoque'")]
        public async Task GerarPlanilhaEstoqueAsync_WhenReportsServiceFails_ShouldRecordIntegrationFailure()
        {
            // Arrange
            _relatoriosApiMock.GerarPlanilhaEstoqueAsync(Arg.Any<SolicitacaoPlanilhaEstoque>(), Arg.Any<CancellationToken>())
                .Returns(RespostasApi.FalhaRede<Stream>(new HttpRequestException("Conexão recusada")));

            // Act
            await _servicoRelatorios.GerarPlanilhaEstoqueAsync(CancellationToken.None);

            // Assert
            _coletor.Medicoes("concessionaria.relatorios.falhas_integracao").ShouldHaveSingleItem().Tags["operacao"].ShouldBe("planilha_estoque");
        }

        [Fact(DisplayName = "ERRO - Não deve contar falha de integração quando o período é recusado antes de chamar Relatórios")]
        public async Task GerarPlanilhaVendasAsync_WhenPeriodIsInvalid_ShouldNotRecordFailure()
        {
            // Act
            await _servicoRelatorios.GerarPlanilhaVendasAsync(new FiltroPeriodo(new DateOnly(2026, 9, 30), new DateOnly(2026, 9, 1)), CancellationToken.None);

            // Assert
            _coletor.Medicoes("concessionaria.relatorios.falhas_integracao").ShouldBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve abrir o span 'Consultar estoque e vendas' com a quantidade de vendas, separado da chamada a Relatórios")]
        public async Task ObterResumoAsync_WhenCalled_ShouldEmitLocalStepSpanWithSalesCount()
        {
            // Arrange
            _repositorioVendasMock.ListarPorPeriodoAsync(null, null, Arg.Any<CancellationToken>()).Returns(VendaDataBuilder.AsList(4));
            _relatoriosApiMock.CalcularIndicadoresVendasAsync(Arg.Any<SolicitacaoIndicadoresVendas>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    // A chamada a Relatórios precisa acontecer depois do span local, e não dentro dele.
                    Activity.Current?.DisplayName.ShouldNotBe("Consultar estoque e vendas");
                    return RespostasApi.Sucesso(new IndicadoresVendas(4, 1m, 1m, 1m, null));
                });

            // Act
            await _servicoMotos.ObterResumoAsync(CancellationToken.None);

            // Assert
            var span = _coletor.Spans.ShouldHaveSingleItem();
            span.DisplayName.ShouldBe("Consultar estoque e vendas");
            span.GetTagItem("concessionaria.quantidade_vendas").ShouldBe(4);
        }
    }
}
