namespace Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;

/// <summary>Veículo do estoque enviado por Carros ou Motos para a planilha de estoque.</summary>
/// <param name="Marca">Fabricante.</param>
/// <param name="Modelo">Modelo.</param>
/// <param name="Ano">Ano do modelo.</param>
/// <param name="Cor">Cor.</param>
/// <param name="Detalhe">Atributo próprio do tipo de veículo (portas ou cilindrada), em texto.</param>
/// <param name="Preco">Preço anunciado.</param>
/// <param name="CadastradoEm">Data de cadastro (UTC).</param>
public record VeiculoRelatorio(string Marca, string Modelo, int Ano, string Cor, string Detalhe, decimal Preco, DateTime CadastradoEm);
