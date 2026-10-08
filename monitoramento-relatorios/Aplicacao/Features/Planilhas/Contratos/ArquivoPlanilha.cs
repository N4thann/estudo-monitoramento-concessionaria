namespace Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;

/// <summary>Planilha gerada, pronta para devolver como download.</summary>
/// <param name="Conteudo">Bytes do arquivo .xlsx.</param>
/// <param name="NomeArquivo">Nome sugerido para o download.</param>
public record ArquivoPlanilha(byte[] Conteudo, string NomeArquivo)
{
    /// <summary>Tipo de conteúdo (MIME) de planilha .xlsx.</summary>
    public const string TipoConteudoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
