using Concessionaria.Relatorios.Aplicacao.Comum.Contratos;

namespace Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;

/// <summary>Pedido da planilha de vendas de um período.</summary>
/// <param name="TipoVeiculo">Rótulo do tipo de veículo ("Carros" ou "Motos"), usado no conteúdo e no nome do arquivo.</param>
/// <param name="De">Primeiro dia do período.</param>
/// <param name="Ate">Último dia do período.</param>
/// <param name="Vendas">Vendas do período; obrigatória, mas pode ser vazia.</param>
public record SolicitacaoPlanilhaVendas(string TipoVeiculo, DateOnly De, DateOnly Ate, List<VendaRelatorio>? Vendas)
{
    /// <summary>Valida os campos; dicionário vazio significa pedido válido. A chave é o nome do campo.</summary>
    public Dictionary<string, string[]> Validar()
    {
        var erros = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(TipoVeiculo))
            erros[nameof(TipoVeiculo)] = ["Tipo de veículo é obrigatório."];
        if (De > Ate)
            erros["Periodo"] = ["A data inicial deve ser anterior ou igual à final."];
        if (Vendas is null)
            erros[nameof(Vendas)] = ["A lista de vendas é obrigatória (pode ser vazia)."];
        return erros;
    }
}
