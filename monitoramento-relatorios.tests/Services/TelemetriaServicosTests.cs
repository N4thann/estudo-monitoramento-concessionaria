using Concessionaria.Relatorios.Aplicacao.Features.Indicadores;
using Concessionaria.Relatorios.Aplicacao.Features.Planilhas;
using Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;
using Concessionaria.Relatorios.tests.Common.Observabilidade;
using Concessionaria.Relatorios.tests.DataBuilder;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Services
{
    /// <summary>Métricas de negócio e spans próprios que alimentam o dashboard e os alertas.</summary>
    [Collection(ColecaoTelemetria.Nome)]
    public class TelemetriaServicosTests : IDisposable
    {
        private readonly ColetorTelemetria _coletor;
        private readonly CalculadoraIndicadores _calculadora;
        private readonly GeradorPlanilhas _sut;

        public TelemetriaServicosTests()
        {
            _coletor = new ColetorTelemetria();
            _calculadora = new CalculadoraIndicadores(Substitute.For<ILogger<CalculadoraIndicadores>>());
            _sut = new GeradorPlanilhas(_calculadora, Substitute.For<ILogger<GeradorPlanilhas>>());
        }

        public void Dispose() => _coletor.Dispose();

        [Fact(DisplayName = "SUCESSO - Deve contar um cálculo de indicadores por chamada")]
        public void CalcularVendas_WhenCalled_ShouldIncrementCalculatedIndicatorsCounter()
        {
            // Act
            _calculadora.CalcularVendas(VendaRelatorioDataBuilder.AsList(3));

            // Assert
            var medicoes = _coletor.Medicoes("concessionaria.indicadores.calculados");
            medicoes.Count.ShouldBe(1);
            medicoes[0].Valor.ShouldBe(1);
        }

        [Fact(DisplayName = "SUCESSO - Deve registrar contagem, linhas e duração da planilha de vendas com tipo de relatório e de veículo")]
        public void GerarVendas_WhenCalled_ShouldRecordSpreadsheetMetricsWithTags()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), VendaRelatorioDataBuilder.AsList(4));

            // Act
            _sut.GerarVendas(solicitacao);

            // Assert
            var geradas = _coletor.Medicoes("concessionaria.planilhas.geradas").ShouldHaveSingleItem();
            geradas.Valor.ShouldBe(1);
            geradas.Tags["tipo_relatorio"].ShouldBe("vendas");
            geradas.Tags["tipo_veiculo"].ShouldBe("carros");

            _coletor.Medicoes("concessionaria.planilhas.linhas").ShouldHaveSingleItem().Valor.ShouldBe(4);
            _coletor.Medicoes("concessionaria.planilhas.duracao").ShouldHaveSingleItem().Valor.ShouldBeGreaterThan(0);
        }

        [Fact(DisplayName = "SUCESSO - Deve registrar as métricas da planilha de estoque com tipo de veículo em minúsculas")]
        public void GerarEstoque_WhenCalled_ShouldRecordSpreadsheetMetricsWithTags()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Motos", "Cilindrada", VeiculoRelatorioDataBuilder.AsList(2));

            // Act
            _sut.GerarEstoque(solicitacao);

            // Assert
            var geradas = _coletor.Medicoes("concessionaria.planilhas.geradas").ShouldHaveSingleItem();
            geradas.Tags["tipo_relatorio"].ShouldBe("estoque");
            geradas.Tags["tipo_veiculo"].ShouldBe("motos");
            _coletor.Medicoes("concessionaria.planilhas.linhas").ShouldHaveSingleItem().Valor.ShouldBe(2);
        }

        [Fact(DisplayName = "SUCESSO - Deve registrar zero linhas quando a lista de vendas é nula")]
        public void GerarVendas_WhenSalesListIsNull_ShouldRecordZeroLines()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), null);

            // Act
            _sut.GerarVendas(solicitacao);

            // Assert
            _coletor.Medicoes("concessionaria.planilhas.linhas").ShouldHaveSingleItem().Valor.ShouldBe(0);
        }

        [Fact(DisplayName = "SUCESSO - Deve abrir o span da geração da planilha de vendas com tipo, linhas e tamanho do arquivo")]
        public void GerarVendas_WhenCalled_ShouldEmitSpanWithTags()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30), VendaRelatorioDataBuilder.AsList(3));

            // Act
            var arquivo = _sut.GerarVendas(solicitacao);

            // Assert
            var span = _coletor.Spans.ShouldHaveSingleItem();
            span.DisplayName.ShouldBe("Gerar planilha de vendas");
            span.GetTagItem("concessionaria.tipo_veiculo").ShouldBe("Carros");
            span.GetTagItem("concessionaria.linhas").ShouldBe(3);
            span.GetTagItem("concessionaria.bytes").ShouldBe(arquivo.Conteudo.Length);
        }

        [Fact(DisplayName = "SUCESSO - Deve abrir o span da geração da planilha de estoque")]
        public void GerarEstoque_WhenCalled_ShouldEmitSpanWithTags()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Motos", "Cilindrada", VeiculoRelatorioDataBuilder.AsList(1));

            // Act
            _sut.GerarEstoque(solicitacao);

            // Assert
            var span = _coletor.Spans.ShouldHaveSingleItem();
            span.DisplayName.ShouldBe("Gerar planilha de estoque");
            span.GetTagItem("concessionaria.tipo_veiculo").ShouldBe("Motos");
            span.GetTagItem("concessionaria.linhas").ShouldBe(1);
        }
    }
}
