namespace Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;

/// <summary>Pedido da planilha de vendas de um período.</summary>
/// <param name="TipoVeiculo">Rótulo do tipo de veículo ("Motos"), usado no conteúdo e no nome do arquivo.</param>
/// <param name="De">Primeiro dia do período.</param>
/// <param name="Ate">Último dia do período.</param>
/// <param name="Vendas">Vendas do período.</param>
public record SolicitacaoPlanilhaVendas(string TipoVeiculo, DateOnly De, DateOnly Ate, IReadOnlyList<VendaRelatorio> Vendas);
