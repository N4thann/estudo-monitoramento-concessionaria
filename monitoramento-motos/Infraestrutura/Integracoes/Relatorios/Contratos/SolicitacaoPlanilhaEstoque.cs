namespace Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;

/// <summary>Pedido da planilha do estoque disponível.</summary>
/// <param name="TipoVeiculo">Rótulo do tipo de veículo ("Motos"), usado no conteúdo e no nome do arquivo.</param>
/// <param name="RotuloDetalhe">Título da coluna de <see cref="VeiculoRelatorio.Detalhe"/> ("Cilindrada").</param>
/// <param name="Veiculos">Veículos disponíveis.</param>
public record SolicitacaoPlanilhaEstoque(string TipoVeiculo, string RotuloDetalhe, IReadOnlyList<VeiculoRelatorio> Veiculos);
