namespace Concessionaria.Relatorios.Contratos;

// Relatórios não tem banco: quem chama (Carros ou Motos) envia os dados e recebe de volta
// os indicadores calculados ou a planilha gerada.

public record VendaRelatorio(string Veiculo, string Marca, string Cliente, string Documento, decimal Valor, DateTime VendidaEm);

public record VeiculoRelatorio(string Marca, string Modelo, int Ano, string Cor, string Detalhe, decimal Preco, DateTime CadastradoEm);

public record SolicitacaoIndicadoresVendas(List<VendaRelatorio>? Vendas);

public record IndicadoresVendas(int QuantidadeVendas, decimal Receita, decimal TicketMedio, decimal MaiorVenda, string? MarcaMaisVendida);

public record SolicitacaoPlanilhaVendas(string TipoVeiculo, DateOnly De, DateOnly Ate, List<VendaRelatorio>? Vendas)
{
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

public record SolicitacaoPlanilhaEstoque(string TipoVeiculo, string RotuloDetalhe, List<VeiculoRelatorio>? Veiculos)
{
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
