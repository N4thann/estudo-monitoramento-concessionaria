namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Dados para registrar a venda de um carro ou moto; quem valida é o serviço do veículo.</summary>
public record RequisicaoVenda(string VeiculoId, string NomeCliente, string DocumentoCliente, decimal Valor);
