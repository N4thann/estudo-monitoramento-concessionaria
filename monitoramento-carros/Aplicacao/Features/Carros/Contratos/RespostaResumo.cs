using Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios.Contratos;

namespace Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;

/// <summary>Resumo do estoque e das vendas de carros, consumido pelo painel do BFF.</summary>
/// <param name="TotalVeiculos">Carros cadastrados (disponíveis + vendidos).</param>
/// <param name="Disponiveis">Carros à venda.</param>
/// <param name="Vendidos">Carros vendidos.</param>
/// <param name="ValorEmEstoque">Soma dos preços dos carros disponíveis.</param>
/// <param name="Indicadores">
/// Indicadores de vendas calculados pelo serviço de Relatórios; nulos quando ele está indisponível, e o restante
/// do resumo continua válido (degradação).
/// </param>
public record RespostaResumo(long TotalVeiculos, long Disponiveis, long Vendidos, decimal ValorEmEstoque, IndicadoresVendas? Indicadores);
