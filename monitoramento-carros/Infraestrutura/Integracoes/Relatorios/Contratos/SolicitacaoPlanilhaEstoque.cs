namespace Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios.Contratos;

/// <summary>Pedido da planilha do estoque disponível.</summary>
/// <param name="TipoVeiculo">Rótulo do tipo de veículo ("Carros"), usado no conteúdo e no nome do arquivo.</param>
/// <param name="RotuloDetalhe">Título da coluna de <see cref="VeiculoRelatorio.Detalhe"/> ("Portas").</param>
/// <param name="Veiculos">Veículos disponíveis.</param>
public record SolicitacaoPlanilhaEstoque(string TipoVeiculo, string RotuloDetalhe, IReadOnlyList<VeiculoRelatorio> Veiculos);
