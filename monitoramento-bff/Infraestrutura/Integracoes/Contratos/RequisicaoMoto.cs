namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Dados para cadastrar ou atualizar uma moto; quem valida é a API de Motos.</summary>
public record RequisicaoMoto(string Marca, string Modelo, int Ano, string Cor, int Cilindrada, decimal Preco);
