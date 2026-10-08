namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Venda devolvida pela API de Carros ou de Motos.</summary>
public record Venda(string Id, string VeiculoId, string DescricaoVeiculo, string Marca, string NomeCliente, string DocumentoCliente, decimal Valor, DateTime VendidaEm);
