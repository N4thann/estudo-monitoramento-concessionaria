using ClosedXML.Excel;
using Concessionaria.Relatorios.Contratos;
using Concessionaria.Relatorios.Servicos;
using Concessionaria.Relatorios.tests.DataBuilder;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Services
{
    public class GeradorPlanilhasTests
    {
        private static readonly DateOnly De = new(2026, 9, 1);
        private static readonly DateOnly Ate = new(2026, 9, 30);

        private readonly ILogger<GeradorPlanilhas> _loggerMock;
        private readonly GeradorPlanilhas _sut;

        public GeradorPlanilhasTests()
        {
            _loggerMock = Substitute.For<ILogger<GeradorPlanilhas>>();
            // Calculadora é regra pura sem dependência externa: usada de verdade, só o logger é substituído.
            var calculadora = new CalculadoraIndicadores(Substitute.For<ILogger<CalculadoraIndicadores>>());
            _sut = new GeradorPlanilhas(calculadora, _loggerMock);
        }

        private static XLWorkbook Abrir(ArquivoPlanilha arquivo) => new(new MemoryStream(arquivo.Conteudo));

        [Fact(DisplayName = "SUCESSO - Deve nomear a planilha de vendas com o tipo de veículo em minúsculas e o período")]
        public void GerarVendas_WhenRequestIsValid_ShouldNameFileWithVehicleTypeAndPeriod()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, VendaRelatorioDataBuilder.AsList(2));

            // Act
            var arquivo = _sut.GerarVendas(solicitacao);

            // Assert
            arquivo.NomeArquivo.ShouldBe("relatorio-vendas-carros_20260901_20260930.xlsx");
            arquivo.Conteudo.ShouldNotBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve gerar as abas Resumo e Vendas nessa ordem")]
        public void GerarVendas_WhenRequestIsValid_ShouldCreateSummaryAndSalesSheets()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, VendaRelatorioDataBuilder.AsList(2));

            // Act
            using var pasta = Abrir(_sut.GerarVendas(solicitacao));

            // Assert
            pasta.Worksheets.Select(a => a.Name).ShouldBe(["Resumo", "Vendas"]);
        }

        [Fact(DisplayName = "SUCESSO - Deve preencher o resumo com os indicadores calculados das vendas")]
        public void GerarVendas_WhenThereAreSales_ShouldFillSummaryWithIndicators()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Motos", De, Ate,
            [
                VendaRelatorioDataBuilder.Create().WithMarca("Honda").WithValor(20_000m),
                VendaRelatorioDataBuilder.Create().WithMarca("Honda").WithValor(30_000m),
                VendaRelatorioDataBuilder.Create().WithMarca("Yamaha").WithValor(40_000m)
            ]);

            // Act
            using var pasta = Abrir(_sut.GerarVendas(solicitacao));

            // Assert
            var resumo = pasta.Worksheet("Resumo");
            resumo.Cell(1, 1).GetString().ShouldBe("Indicador");
            resumo.Cell(1, 2).GetString().ShouldBe("Valor");
            resumo.Cell(2, 2).GetString().ShouldBe("Motos");
            resumo.Cell(3, 2).GetString().ShouldBe("01/09/2026 a 30/09/2026");
            resumo.Cell(4, 2).GetValue<int>().ShouldBe(3);
            resumo.Cell(5, 2).GetValue<decimal>().ShouldBe(90_000m);
            resumo.Cell(6, 2).GetValue<decimal>().ShouldBe(30_000m);
            resumo.Cell(7, 2).GetValue<decimal>().ShouldBe(40_000m);
            resumo.Cell(8, 2).GetString().ShouldBe("Honda");
        }

        [Fact(DisplayName = "SUCESSO - Deve escrever uma linha por venda com os dados do cliente e do veículo")]
        public void GerarVendas_WhenThereAreSales_ShouldWriteOneRowPerSale()
        {
            // Arrange
            var venda = VendaRelatorioDataBuilder.Create()
                .WithVeiculo("Corolla").WithMarca("Toyota").WithCliente("Maria Souza")
                .WithDocumento("12345678901").WithValor(150_000.50m)
                .WithVendidaEm(new DateTime(2026, 9, 10, 15, 30, 0, DateTimeKind.Utc))
                .Build();
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, [venda, VendaRelatorioDataBuilder.Create()]);

            // Act
            using var pasta = Abrir(_sut.GerarVendas(solicitacao));

            // Assert
            var aba = pasta.Worksheet("Vendas");
            Enumerable.Range(1, 6).Select(c => aba.Cell(1, c).GetString())
                .ShouldBe(["Data", "Veículo", "Marca", "Cliente", "Documento", "Valor"]);
            aba.LastRowUsed()!.RowNumber().ShouldBe(3);
            aba.Cell(2, 2).GetString().ShouldBe("Corolla");
            aba.Cell(2, 3).GetString().ShouldBe("Toyota");
            aba.Cell(2, 4).GetString().ShouldBe("Maria Souza");
            aba.Cell(2, 5).GetString().ShouldBe("12345678901");
            aba.Cell(2, 6).GetValue<decimal>().ShouldBe(150_000.50m);
        }

        [Fact(DisplayName = "SUCESSO - Deve converter a data da venda de UTC para o horário de Brasília")]
        public void GerarVendas_WhenSaleDateIsUtc_ShouldConvertToBrasiliaTime()
        {
            // Arrange
            var venda = VendaRelatorioDataBuilder.Create().WithVendidaEm(new DateTime(2026, 9, 10, 15, 30, 0, DateTimeKind.Utc)).Build();
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, [venda]);

            // Act
            using var pasta = Abrir(_sut.GerarVendas(solicitacao));

            // Assert
            pasta.Worksheet("Vendas").Cell(2, 1).GetDateTime().ShouldBe(new DateTime(2026, 9, 10, 12, 30, 0));
        }

        [Fact(DisplayName = "SUCESSO - Deve formatar data e valor da venda no padrão brasileiro")]
        public void GerarVendas_WhenThereAreSales_ShouldApplyDateAndCurrencyFormats()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, VendaRelatorioDataBuilder.AsList(1));

            // Act
            using var pasta = Abrir(_sut.GerarVendas(solicitacao));

            // Assert
            var aba = pasta.Worksheet("Vendas");
            aba.Cell(2, 1).Style.NumberFormat.Format.ShouldBe(ExtensoesPlanilha.FormatoDataHora);
            aba.Cell(2, 6).Style.NumberFormat.Format.ShouldBe(ExtensoesPlanilha.FormatoMoeda);
        }

        [Fact(DisplayName = "SUCESSO - Deve gerar planilha só com cabeçalho e resumo zerado quando não há vendas")]
        public void GerarVendas_WhenThereAreNoSales_ShouldGenerateHeaderOnlyAndZeroedSummary()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, []);

            // Act
            using var pasta = Abrir(_sut.GerarVendas(solicitacao));

            // Assert
            pasta.Worksheet("Vendas").LastRowUsed()!.RowNumber().ShouldBe(1);
            var resumo = pasta.Worksheet("Resumo");
            resumo.Cell(4, 2).GetValue<int>().ShouldBe(0);
            resumo.Cell(5, 2).GetValue<decimal>().ShouldBe(0m);
            resumo.Cell(8, 2).GetString().ShouldBe("-");
        }

        [Fact(DisplayName = "SUCESSO - Deve tratar lista de vendas nula como vazia")]
        public void GerarVendas_WhenSalesListIsNull_ShouldTreatAsEmpty()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaVendas("Carros", De, Ate, null);

            // Act
            using var pasta = Abrir(_sut.GerarVendas(solicitacao));

            // Assert
            pasta.Worksheet("Vendas").LastRowUsed()!.RowNumber().ShouldBe(1);
            pasta.Worksheet("Resumo").Cell(4, 2).GetValue<int>().ShouldBe(0);
        }

        [Fact(DisplayName = "SUCESSO - Deve nomear a planilha de estoque com o tipo de veículo e o momento da geração")]
        public void GerarEstoque_WhenRequestIsValid_ShouldNameFileWithVehicleTypeAndTimestamp()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Motos", "Cilindrada", VeiculoRelatorioDataBuilder.AsList(2));

            // Act
            var arquivo = _sut.GerarEstoque(solicitacao);

            // Assert
            arquivo.NomeArquivo.ShouldMatch(@"^relatorio-estoque-motos_\d{8}_\d{4}\.xlsx$");
            arquivo.Conteudo.ShouldNotBeEmpty();
        }

        [Fact(DisplayName = "SUCESSO - Deve gerar as abas Resumo e Estoque nessa ordem")]
        public void GerarEstoque_WhenRequestIsValid_ShouldCreateSummaryAndStockSheets()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "Portas", VeiculoRelatorioDataBuilder.AsList(2));

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            pasta.Worksheets.Select(a => a.Name).ShouldBe(["Resumo", "Estoque"]);
        }

        [Fact(DisplayName = "SUCESSO - Deve preencher o resumo com quantidade, valor em estoque e preço médio arredondado")]
        public void GerarEstoque_WhenThereAreVehicles_ShouldFillSummaryWithStockTotals()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "Portas",
            [
                VeiculoRelatorioDataBuilder.Create().WithPreco(100m),
                VeiculoRelatorioDataBuilder.Create().WithPreco(100m),
                VeiculoRelatorioDataBuilder.Create().WithPreco(100.02m)
            ]);

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            var resumo = pasta.Worksheet("Resumo");
            resumo.Cell(2, 2).GetString().ShouldBe("Carros");
            resumo.Cell(3, 2).GetValue<int>().ShouldBe(3);
            resumo.Cell(4, 2).GetValue<decimal>().ShouldBe(300.02m);
            resumo.Cell(5, 2).GetValue<decimal>().ShouldBe(100.01m);
        }

        [Fact(DisplayName = "SUCESSO - Deve usar o rótulo do detalhe recebido como título da coluna")]
        public void GerarEstoque_WhenDetailLabelIsInformed_ShouldUseItAsColumnHeader()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Motos", "Cilindrada", VeiculoRelatorioDataBuilder.AsList(1));

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            var aba = pasta.Worksheet("Estoque");
            Enumerable.Range(1, 7).Select(c => aba.Cell(1, c).GetString())
                .ShouldBe(["Marca", "Modelo", "Ano", "Cor", "Cilindrada", "Preço", "Cadastrado em"]);
        }

        [Fact(DisplayName = "SUCESSO - Deve escrever uma linha por veículo com o cadastro convertido para Brasília")]
        public void GerarEstoque_WhenThereAreVehicles_ShouldWriteOneRowPerVehicle()
        {
            // Arrange
            var veiculo = VeiculoRelatorioDataBuilder.Create()
                .WithMarca("Honda").WithModelo("CB 500").WithAno(2024).WithCor("Vermelha")
                .WithDetalhe("500").WithPreco(42_000m)
                .WithCadastradoEm(new DateTime(2026, 1, 5, 3, 0, 0, DateTimeKind.Utc))
                .Build();
            var solicitacao = new SolicitacaoPlanilhaEstoque("Motos", "Cilindrada", [veiculo, VeiculoRelatorioDataBuilder.Create()]);

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            var aba = pasta.Worksheet("Estoque");
            aba.LastRowUsed()!.RowNumber().ShouldBe(3);
            aba.Cell(2, 1).GetString().ShouldBe("Honda");
            aba.Cell(2, 2).GetString().ShouldBe("CB 500");
            aba.Cell(2, 3).GetValue<int>().ShouldBe(2024);
            aba.Cell(2, 4).GetString().ShouldBe("Vermelha");
            aba.Cell(2, 6).GetValue<decimal>().ShouldBe(42_000m);
            aba.Cell(2, 7).GetDateTime().ShouldBe(new DateTime(2026, 1, 5, 0, 0, 0));
        }

        [Fact(DisplayName = "SUCESSO - Deve gravar o detalhe como número quando ele é numérico")]
        public void GerarEstoque_WhenDetailIsNumeric_ShouldWriteNumberCell()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "Portas", [VeiculoRelatorioDataBuilder.Create().WithDetalhe("4")]);

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            var celula = pasta.Worksheet("Estoque").Cell(2, 5);
            celula.DataType.ShouldBe(XLDataType.Number);
            celula.GetValue<int>().ShouldBe(4);
        }

        [Fact(DisplayName = "SUCESSO - Deve gravar o detalhe como texto quando ele não é numérico")]
        public void GerarEstoque_WhenDetailIsNotNumeric_ShouldWriteTextCell()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Motos", "Cilindrada", [VeiculoRelatorioDataBuilder.Create().WithDetalhe("Elétrica")]);

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            var celula = pasta.Worksheet("Estoque").Cell(2, 5);
            celula.DataType.ShouldBe(XLDataType.Text);
            celula.GetString().ShouldBe("Elétrica");
        }

        [Fact(DisplayName = "SUCESSO - Deve formatar preço e data de cadastro no padrão brasileiro")]
        public void GerarEstoque_WhenThereAreVehicles_ShouldApplyCurrencyAndDateFormats()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "Portas", VeiculoRelatorioDataBuilder.AsList(1));

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            var aba = pasta.Worksheet("Estoque");
            aba.Cell(2, 6).Style.NumberFormat.Format.ShouldBe(ExtensoesPlanilha.FormatoMoeda);
            aba.Cell(2, 7).Style.NumberFormat.Format.ShouldBe(ExtensoesPlanilha.FormatoDataHora);
        }

        [Fact(DisplayName = "SUCESSO - Deve gerar resumo zerado sem dividir por zero quando o estoque está vazio")]
        public void GerarEstoque_WhenThereAreNoVehicles_ShouldGenerateZeroedSummary()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "Portas", []);

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            pasta.Worksheet("Estoque").LastRowUsed()!.RowNumber().ShouldBe(1);
            var resumo = pasta.Worksheet("Resumo");
            resumo.Cell(3, 2).GetValue<int>().ShouldBe(0);
            resumo.Cell(4, 2).GetValue<decimal>().ShouldBe(0m);
            resumo.Cell(5, 2).GetValue<decimal>().ShouldBe(0m);
        }

        [Fact(DisplayName = "SUCESSO - Deve tratar lista de veículos nula como vazia")]
        public void GerarEstoque_WhenVehicleListIsNull_ShouldTreatAsEmpty()
        {
            // Arrange
            var solicitacao = new SolicitacaoPlanilhaEstoque("Carros", "Portas", null);

            // Act
            using var pasta = Abrir(_sut.GerarEstoque(solicitacao));

            // Assert
            pasta.Worksheet("Estoque").LastRowUsed()!.RowNumber().ShouldBe(1);
            pasta.Worksheet("Resumo").Cell(3, 2).GetValue<int>().ShouldBe(0);
        }
    }
}
