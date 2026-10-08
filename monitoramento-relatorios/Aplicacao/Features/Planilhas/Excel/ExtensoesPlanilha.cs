using ClosedXML.Excel;

namespace Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Excel;

/// <summary>Montagem das abas com ClosedXML: cabeçalho destacado e congelado, formatos por coluna e largura ajustada.</summary>
public static class ExtensoesPlanilha
{
    /// <summary>Formato de moeda em reais.</summary>
    public const string FormatoMoeda = "R$ #,##0.00";

    /// <summary>Formato de data e hora brasileiro.</summary>
    public const string FormatoDataHora = "dd/MM/yyyy HH:mm";

    /// <summary>Adiciona uma aba com o cabeçalho na linha 1 e uma linha de dados por item de <paramref name="linhas"/>.</summary>
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

    /// <summary>Salva a pasta de trabalho em memória e devolve os bytes do .xlsx.</summary>
    public static byte[] ParaBytes(this XLWorkbook pasta)
    {
        using var memoria = new MemoryStream();
        pasta.SaveAs(memoria);
        return memoria.ToArray();
    }
}
