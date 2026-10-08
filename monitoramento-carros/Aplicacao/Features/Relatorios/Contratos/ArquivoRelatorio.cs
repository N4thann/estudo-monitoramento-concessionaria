namespace Concessionaria.Carros.Aplicacao.Features.Relatorios.Contratos;

/// <summary>Planilha gerada pelo serviço de Relatórios, pronta para devolver como download.</summary>
/// <param name="Conteudo">Bytes do arquivo .xlsx.</param>
/// <param name="NomeArquivo">Nome sugerido para o download.</param>
public record ArquivoRelatorio(byte[] Conteudo, string NomeArquivo)
{
    /// <summary>Tipo de conteúdo (MIME) de planilha .xlsx.</summary>
    public const string TipoConteudoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
