using ClosedXML.Excel;

namespace Concessionaria.Relatorios.Servicos;

public record ColunaPlanilha(string Titulo, string? Formato = null);

public static class ExtensoesPlanilha
{
    public const string FormatoMoeda = "R$ #,##0.00";
    public const string FormatoDataHora = "dd/MM/yyyy HH:mm";

    public static IXLWorksheet AdicionarAba(this XLWorkbook pasta, string nome, IReadOnlyList<ColunaPlanilha> colunas, IEnumerable<object?[]> linhas)
    {
        var aba = pasta.Worksheets.Add(nome);

        for (var coluna = 0; coluna < colunas.Count; coluna++)
            aba.Cell(1, coluna + 1).Value = colunas[coluna].Titulo;

        var cabecalho = aba.Range(1, 1, 1, colunas.Count);
        cabecalho.Style.Font.Bold = true;
        cabecalho.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E40AF");
        cabecalho.Style.Font.FontColor = XLColor.White;

        var linha = 2;
        foreach (var valores in linhas)
        {
            for (var coluna = 0; coluna < colunas.Count; coluna++)
            {
                var celula = aba.Cell(linha, coluna + 1);
                celula.Value = XLCellValue.FromObject(valores[coluna]);
                if (colunas[coluna].Formato is { } formato)
                    celula.Style.NumberFormat.Format = formato;
            }
            linha++;
        }

        aba.SheetView.FreezeRows(1);
        aba.Columns().AdjustToContents();
        return aba;
    }

    public static byte[] ParaBytes(this XLWorkbook pasta)
    {
        using var memoria = new MemoryStream();
        pasta.SaveAs(memoria);
        return memoria.ToArray();
    }
}
