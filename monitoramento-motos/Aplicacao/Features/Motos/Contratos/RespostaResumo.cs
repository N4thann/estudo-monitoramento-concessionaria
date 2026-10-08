using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;

namespace Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;

/// <summary>Resumo do estoque e das vendas de motos, consumido pelo painel do BFF.</summary>
/// <param name="TotalVeiculos">Motos cadastradas (disponíveis + vendidas).</param>
/// <param name="Disponiveis">Motos à venda.</param>
/// <param name="Vendidos">Motos vendidas.</param>
/// <param name="ValorEmEstoque">Soma dos preços das motos disponíveis.</param>
/// <param name="Indicadores">
/// Indicadores de vendas calculados pelo serviço de Relatórios; nulos quando ele está indisponível, e o restante
/// do resumo continua válido (degradação).
/// </param>
public record RespostaResumo(long TotalVeiculos, long Disponiveis, long Vendidos, decimal ValorEmEstoque, IndicadoresVendas? Indicadores);
