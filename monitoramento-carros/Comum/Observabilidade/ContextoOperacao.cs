namespace Concessionaria.Carros.Comum.Observabilidade;

/// <summary>
/// Id de operação da requisição atual. Vem do cabeçalho X-Id-Operacao (ou é gerado na borda) e segue
/// para os serviços chamados, permitindo correlacionar nos logs uma mesma operação entre as APIs.
/// </summary>
public static class ContextoOperacao
{
    /// <summary>Cabeçalho HTTP que carrega o Id de operação, na entrada e na saída.</summary>
    public const string Cabecalho = "X-Id-Operacao";

    /// <summary>Nome da propriedade estruturada de log com o Id de operação.</summary>
    public const string PropriedadeLog = "IdOperacao";

    private static readonly AsyncLocal<string?> _idAtual = new();

    /// <summary>Id de operação do fluxo assíncrono atual; nulo fora de uma requisição.</summary>
    public static string? IdAtual
    {
        get => _idAtual.Value;
        set => _idAtual.Value = value;
    }
}
