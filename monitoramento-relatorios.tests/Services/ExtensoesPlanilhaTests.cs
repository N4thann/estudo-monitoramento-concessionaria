using ClosedXML.Excel;
using Concessionaria.Relatorios.Servicos;
using Shouldly;

namespace Concessionaria.Relatorios.tests.Services
{
    public class ExtensoesPlanilhaTests
    {
        private static readonly ColunaPlanilha[] Colunas = [new("Nome"), new("Valor", ExtensoesPlanilha.FormatoMoeda)];

        [Fact(DisplayName = "SUCESSO - Deve criar a aba com o nome e os títulos das colunas na primeira linha")]
        public void AdicionarAba_WhenCalled_ShouldWriteHeaderOnFirstRow()
        {
            // Arrange
            using var pasta = new XLWorkbook();

            // Act
            var aba = pasta.AdicionarAba("Dados", Colunas, []);

            // Assert
            aba.Name.ShouldBe("Dados");
            aba.Cell(1, 1).GetString().ShouldBe("Nome");
            aba.Cell(1, 2).GetString().ShouldBe("Valor");
        }

        [Fact(DisplayName = "SUCESSO - Deve destacar o cabeçalho em negrito, fundo azul e fonte branca")]
        public void AdicionarAba_WhenCalled_ShouldStyleHeader()
        {
            // Arrange
            using var pasta = new XLWorkbook();

            // Act
            var aba = pasta.AdicionarAba("Dados", Colunas, []);

            // Assert
            foreach (var coluna in new[] { 1, 2 })
            {
                var estilo = aba.Cell(1, coluna).Style;
                estilo.Font.Bold.ShouldBeTrue();
                estilo.Fill.BackgroundColor.ShouldBe(XLColor.FromHtml("#1E40AF"));
                estilo.Font.FontColor.ShouldBe(XLColor.White);
            }
        }

        [Fact(DisplayName = "SUCESSO - Deve congelar a linha do cabeçalho")]
        public void AdicionarAba_WhenCalled_ShouldFreezeHeaderRow()
        {
            // Arrange
            using var pasta = new XLWorkbook();

            // Act
            var aba = pasta.AdicionarAba("Dados", Colunas, []);

            // Assert
            aba.SheetView.SplitRow.ShouldBe(1);
        }

        [Fact(DisplayName = "SUCESSO - Deve escrever os dados a partir da segunda linha, na ordem recebida")]
        public void AdicionarAba_WhenThereAreRows_ShouldWriteDataFromSecondRow()
        {
            // Arrange
            using var pasta = new XLWorkbook();

            // Act
            var aba = pasta.AdicionarAba("Dados", Colunas, [["Ana", 10.5m], ["Bruno", 20m]]);

            // Assert
            aba.LastRowUsed()!.RowNumber().ShouldBe(3);
            aba.Cell(2, 1).GetString().ShouldBe("Ana");
            aba.Cell(2, 2).GetValue<decimal>().ShouldBe(10.5m);
            aba.Cell(3, 1).GetString().ShouldBe("Bruno");
            aba.Cell(3, 2).GetValue<decimal>().ShouldBe(20m);
        }

        [Fact(DisplayName = "SUCESSO - Deve aplicar o formato só nas colunas que definem formato")]
        public void AdicionarAba_WhenColumnHasFormat_ShouldApplyItOnlyToThatColumn()
        {
            // Arrange
            using var pasta = new XLWorkbook();

            // Act
            var aba = pasta.AdicionarAba("Dados", Colunas, [["Ana", 10.5m]]);

            // Assert
            aba.Cell(2, 2).Style.NumberFormat.Format.ShouldBe(ExtensoesPlanilha.FormatoMoeda);
            aba.Cell(2, 1).Style.NumberFormat.Format.ShouldNotBe(ExtensoesPlanilha.FormatoMoeda);
        }

        [Fact(DisplayName = "SUCESSO - Deve deixar a célula em branco quando o valor é nulo")]
        public void AdicionarAba_WhenValueIsNull_ShouldLeaveCellBlank()
        {
            // Arrange
            using var pasta = new XLWorkbook();

            // Act
            var aba = pasta.AdicionarAba("Dados", Colunas, [[null, 10m]]);

            // Assert
            aba.Cell(2, 1).DataType.ShouldBe(XLDataType.Blank);
        }

        [Fact(DisplayName = "SUCESSO - Deve serializar a pasta em um .xlsx que pode ser aberto de novo")]
        public void ParaBytes_WhenWorkbookHasSheets_ShouldProduceReadableXlsx()
        {
            // Arrange
            using var pasta = new XLWorkbook();
            pasta.AdicionarAba("Dados", Colunas, [["Ana", 10.5m]]);

            // Act
            var bytes = pasta.ParaBytes();

            // Assert
            bytes.Take(2).ShouldBe("PK"u8.ToArray());
            using var reaberta = new XLWorkbook(new MemoryStream(bytes));
            reaberta.Worksheet("Dados").Cell(2, 1).GetString().ShouldBe("Ana");
        }
    }
}
