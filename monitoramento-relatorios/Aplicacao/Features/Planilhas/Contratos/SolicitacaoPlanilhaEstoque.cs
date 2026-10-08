namespace Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;

/// <summary>Pedido da planilha do estoque disponível.</summary>
/// <param name="TipoVeiculo">Rótulo do tipo de veículo ("Carros" ou "Motos"), usado no conteúdo e no nome do arquivo.</param>
/// <param name="RotuloDetalhe">Título da coluna de <see cref="VeiculoRelatorio.Detalhe"/> ("Portas" ou "Cilindrada").</param>
/// <param name="Veiculos">Veículos disponíveis; obrigatória, mas pode ser vazia.</param>
public record SolicitacaoPlanilhaEstoque(string TipoVeiculo, string RotuloDetalhe, List<VeiculoRelatorio>? Veiculos)
{
    /// <summary>Valida os campos; dicionário vazio significa pedido válido. A chave é o nome do campo.</summary>
    public Dictionary<string, string[]> Validar()
    {
        var erros = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(TipoVeiculo))
            erros[nameof(TipoVeiculo)] = ["Tipo de veículo é obrigatório."];
        if (string.IsNullOrWhiteSpace(RotuloDetalhe))
            erros[nameof(RotuloDetalhe)] = ["Rótulo do detalhe do veículo é obrigatório."];
        if (Veiculos is null)
            erros[nameof(Veiculos)] = ["A lista de veículos é obrigatória (pode ser vazia)."];
        return erros;
    }
}
