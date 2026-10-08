namespace Concessionaria.Bff.Aplicacao.Features.Painel.Contratos;

/// <summary>Soma dos resumos de Carros e Motos.</summary>
/// <param name="TotalVeiculos">Veículos cadastrados nos dois serviços.</param>
/// <param name="Disponiveis">Veículos à venda.</param>
/// <param name="Vendidos">Veículos vendidos.</param>
/// <param name="ValorEmEstoque">Soma dos preços dos disponíveis.</param>
/// <param name="QuantidadeVendas">Vendas dos dois serviços; nula quando algum veio sem indicadores (Relatórios fora).</param>
/// <param name="Receita">Receita dos dois serviços; nula quando algum veio sem indicadores (Relatórios fora).</param>
public record ResumoGeral(long TotalVeiculos, long Disponiveis, long Vendidos, decimal ValorEmEstoque, int? QuantidadeVendas, decimal? Receita);
