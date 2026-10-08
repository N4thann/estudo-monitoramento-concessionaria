namespace Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;

/// <summary>Veículo do estoque no formato que Relatórios espera.</summary>
/// <param name="Marca">Fabricante.</param>
/// <param name="Modelo">Modelo.</param>
/// <param name="Ano">Ano do modelo.</param>
/// <param name="Cor">Cor.</param>
/// <param name="Detalhe">Atributo próprio do tipo de veículo (aqui, a cilindrada), em texto.</param>
/// <param name="Preco">Preço anunciado.</param>
/// <param name="CadastradoEm">Data de cadastro (UTC).</param>
public record VeiculoRelatorio(string Marca, string Modelo, int Ano, string Cor, string Detalhe, decimal Preco, DateTime CadastradoEm);
