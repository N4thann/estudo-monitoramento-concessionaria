namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Resumo do estoque e das vendas de um serviço (Carros ou Motos).</summary>
/// <param name="TotalVeiculos">Veículos cadastrados.</param>
/// <param name="Disponiveis">Veículos à venda.</param>
/// <param name="Vendidos">Veículos vendidos.</param>
/// <param name="ValorEmEstoque">Soma dos preços dos disponíveis.</param>
/// <param name="Indicadores">Indicadores de vendas; nulos quando Relatórios está indisponível.</param>
public record Resumo(long TotalVeiculos, long Disponiveis, long Vendidos, decimal ValorEmEstoque, IndicadoresVendas? Indicadores);
