namespace Concessionaria.Carros.Comum.Observabilidade;

// Id de operação da requisição atual. Vem do cabeçalho X-Id-Operacao (ou é gerado na borda) e segue
// para os serviços chamados, permitindo correlacionar nos logs uma mesma operação entre as APIs.
public static class ContextoOperacao
{
    public const string Cabecalho = "X-Id-Operacao";
    public const string PropriedadeLog = "IdOperacao";

    private static readonly AsyncLocal<string?> _idAtual = new();

    public static string? IdAtual
    {
        get => _idAtual.Value;
        set => _idAtual.Value = value;
    }
}
