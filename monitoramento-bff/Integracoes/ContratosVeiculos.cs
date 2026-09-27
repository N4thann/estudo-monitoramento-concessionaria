using Refit;

namespace Concessionaria.Bff.Integracoes;

// Contratos das APIs de Carros e Motos, do ponto de vista do BFF.

public enum SituacaoVeiculo
{
    Disponivel,
    Vendido
}

public record ResultadoPaginado<T>(List<T> Itens, long Total, int Pagina, int TamanhoPagina);

public record FiltroVeiculos(int? Pagina, int? TamanhoPagina, SituacaoVeiculo? Situacao, string? Busca);

public record FiltroPeriodo(
    [property: Query(Format = "yyyy-MM-dd")] DateOnly? De,
    [property: Query(Format = "yyyy-MM-dd")] DateOnly? Ate);

public record Carro(string Id, string Marca, string Modelo, int Ano, string Cor, int Portas, decimal Preco, SituacaoVeiculo Situacao, DateTime CriadoEm);

public record RequisicaoCarro(string Marca, string Modelo, int Ano, string Cor, int Portas, decimal Preco);

public record Moto(string Id, string Marca, string Modelo, int Ano, string Cor, int Cilindrada, decimal Preco, SituacaoVeiculo Situacao, DateTime CriadoEm);

public record RequisicaoMoto(string Marca, string Modelo, int Ano, string Cor, int Cilindrada, decimal Preco);

public record RequisicaoVenda(string VeiculoId, string NomeCliente, string DocumentoCliente, decimal Valor);

public record Venda(string Id, string VeiculoId, string DescricaoVeiculo, string Marca, string NomeCliente, string DocumentoCliente, decimal Valor, DateTime VendidaEm);

public record IndicadoresVendas(int QuantidadeVendas, decimal Receita, decimal TicketMedio, decimal MaiorVenda, string? MarcaMaisVendida);

public record Resumo(long TotalVeiculos, long Disponiveis, long Vendidos, decimal ValorEmEstoque, IndicadoresVendas? Indicadores);
