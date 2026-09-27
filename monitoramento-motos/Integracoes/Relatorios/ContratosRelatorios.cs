namespace Concessionaria.Motos.Integracoes.Relatorios;

// Contrato HTTP da API de Relatórios. Ela não tem banco: recebe os dados de quem pede e devolve
// indicadores calculados ou a planilha gerada.

public record VendaRelatorio(string Veiculo, string Marca, string Cliente, string Documento, decimal Valor, DateTime VendidaEm);

public record VeiculoRelatorio(string Marca, string Modelo, int Ano, string Cor, string Detalhe, decimal Preco, DateTime CadastradoEm);

public record SolicitacaoIndicadoresVendas(IReadOnlyList<VendaRelatorio> Vendas);

public record IndicadoresVendas(int QuantidadeVendas, decimal Receita, decimal TicketMedio, decimal MaiorVenda, string? MarcaMaisVendida);

public record SolicitacaoPlanilhaVendas(string TipoVeiculo, DateOnly De, DateOnly Ate, IReadOnlyList<VendaRelatorio> Vendas);

public record SolicitacaoPlanilhaEstoque(string TipoVeiculo, string RotuloDetalhe, IReadOnlyList<VeiculoRelatorio> Veiculos);
