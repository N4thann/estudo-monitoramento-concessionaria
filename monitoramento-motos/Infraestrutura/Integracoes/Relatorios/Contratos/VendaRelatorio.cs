namespace Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;

/// <summary>Venda no formato que Relatórios espera.</summary>
/// <param name="Veiculo">Descrição do veículo ("Marca Modelo Ano").</param>
/// <param name="Marca">Marca do veículo.</param>
/// <param name="Cliente">Nome do comprador.</param>
/// <param name="Documento">CPF ou CNPJ do comprador.</param>
/// <param name="Valor">Valor da venda.</param>
/// <param name="VendidaEm">Data da venda (UTC).</param>
public record VendaRelatorio(string Veiculo, string Marca, string Cliente, string Documento, decimal Valor, DateTime VendidaEm);
