namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Dados para cadastrar ou atualizar um carro; quem valida é a API de Carros.</summary>
public record RequisicaoCarro(string Marca, string Modelo, int Ano, string Cor, int Portas, decimal Preco);
