namespace Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios.Contratos;

/// <summary>Pedido de cálculo dos indicadores de vendas.</summary>
/// <param name="Vendas">Vendas consideradas no cálculo.</param>
public record SolicitacaoIndicadoresVendas(IReadOnlyList<VendaRelatorio> Vendas);
