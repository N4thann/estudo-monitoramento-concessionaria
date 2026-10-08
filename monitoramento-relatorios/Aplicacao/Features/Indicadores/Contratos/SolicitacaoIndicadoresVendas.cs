using Concessionaria.Relatorios.Aplicacao.Comum.Contratos;

namespace Concessionaria.Relatorios.Aplicacao.Features.Indicadores.Contratos;

/// <summary>Pedido de cálculo dos indicadores de vendas.</summary>
/// <param name="Vendas">Vendas consideradas; lista ausente é tratada como vazia.</param>
public record SolicitacaoIndicadoresVendas(List<VendaRelatorio>? Vendas);
