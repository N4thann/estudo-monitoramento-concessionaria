using Concessionaria.Relatorios.Aplicacao.Comum.Contratos;
using Concessionaria.Relatorios.Aplicacao.Features.Indicadores;
using Concessionaria.Relatorios.tests.DataBuilder;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Services
{
    public class CalculadoraIndicadoresTests
    {
        private readonly ILogger<CalculadoraIndicadores> _loggerMock;
        private readonly CalculadoraIndicadores _sut;

        public CalculadoraIndicadoresTests()
        {
            _loggerMock = Substitute.For<ILogger<CalculadoraIndicadores>>();
            _sut = new CalculadoraIndicadores(_loggerMock);
        }

        [Fact(DisplayName = "SUCESSO - Deve devolver indicadores zerados e sem marca quando não há vendas")]
        public void CalcularVendas_WhenThereAreNoSales_ShouldReturnZeroedIndicators()
        {
            // Act
            var indicadores = _sut.CalcularVendas([]);

            // Assert
            indicadores.QuantidadeVendas.ShouldBe(0);
            indicadores.Receita.ShouldBe(0m);
            indicadores.TicketMedio.ShouldBe(0m);
            indicadores.MaiorVenda.ShouldBe(0m);
            indicadores.MarcaMaisVendida.ShouldBeNull();
        }

        [Fact(DisplayName = "SUCESSO - Deve usar a própria venda como receita, ticket e maior venda quando há uma única venda")]
        public void CalcularVendas_WhenThereIsASingleSale_ShouldUseItsValueForEveryIndicator()
        {
            // Arrange
            var venda = VendaRelatorioDataBuilder.Create().WithMarca("Toyota").WithValor(150_000m).Build();

            // Act
            var indicadores = _sut.CalcularVendas([venda]);

            // Assert
            indicadores.QuantidadeVendas.ShouldBe(1);
            indicadores.Receita.ShouldBe(150_000m);
            indicadores.TicketMedio.ShouldBe(150_000m);
            indicadores.MaiorVenda.ShouldBe(150_000m);
            indicadores.MarcaMaisVendida.ShouldBe("Toyota");
        }

        [Fact(DisplayName = "SUCESSO - Deve somar a receita e apontar a maior venda")]
        public void CalcularVendas_WhenThereAreSeveralSales_ShouldSumRevenueAndFindHighestSale()
        {
            // Arrange
            List<VendaRelatorio> vendas =
            [
                VendaRelatorioDataBuilder.Create().WithValor(80_000m),
                VendaRelatorioDataBuilder.Create().WithValor(120_000m),
                VendaRelatorioDataBuilder.Create().WithValor(100_000m)
            ];

            // Act
            var indicadores = _sut.CalcularVendas(vendas);

            // Assert
            indicadores.QuantidadeVendas.ShouldBe(3);
            indicadores.Receita.ShouldBe(300_000m);
            indicadores.TicketMedio.ShouldBe(100_000m);
            indicadores.MaiorVenda.ShouldBe(120_000m);
        }

        [Fact(DisplayName = "SUCESSO - Deve arredondar o ticket médio para duas casas decimais")]
        public void CalcularVendas_WhenAverageHasManyDecimals_ShouldRoundTicketToTwoPlaces()
        {
            // Arrange
            List<VendaRelatorio> vendas =
            [
                VendaRelatorioDataBuilder.Create().WithValor(100m),
                VendaRelatorioDataBuilder.Create().WithValor(100m),
                VendaRelatorioDataBuilder.Create().WithValor(100.02m)
            ];

            // Act
            var indicadores = _sut.CalcularVendas(vendas);

            // Assert
            indicadores.Receita.ShouldBe(300.02m);
            indicadores.TicketMedio.ShouldBe(100.01m);
        }

        [Fact(DisplayName = "SUCESSO - Deve apontar como mais vendida a marca com mais vendas, mesmo com receita menor")]
        public void CalcularVendas_WhenOneBrandHasMoreSales_ShouldPickItAsBestSeller()
        {
            // Arrange
            List<VendaRelatorio> vendas =
            [
                VendaRelatorioDataBuilder.Create().WithMarca("Fiat").WithValor(60_000m),
                VendaRelatorioDataBuilder.Create().WithMarca("Fiat").WithValor(70_000m),
                VendaRelatorioDataBuilder.Create().WithMarca("Toyota").WithValor(500_000m)
            ];

            // Act
            var indicadores = _sut.CalcularVendas(vendas);

            // Assert
            indicadores.MarcaMaisVendida.ShouldBe("Fiat");
        }

        [Fact(DisplayName = "SUCESSO - Deve desempatar a marca mais vendida pela maior receita")]
        public void CalcularVendas_WhenBrandsTieOnSalesCount_ShouldPickTheOneWithHigherRevenue()
        {
            // Arrange
            List<VendaRelatorio> vendas =
            [
                VendaRelatorioDataBuilder.Create().WithMarca("Fiat").WithValor(60_000m),
                VendaRelatorioDataBuilder.Create().WithMarca("Honda").WithValor(90_000m),
                VendaRelatorioDataBuilder.Create().WithMarca("Fiat").WithValor(70_000m),
                VendaRelatorioDataBuilder.Create().WithMarca("Honda").WithValor(95_000m)
            ];

            // Act
            var indicadores = _sut.CalcularVendas(vendas);

            // Assert
            indicadores.MarcaMaisVendida.ShouldBe("Honda");
        }
    }
}
