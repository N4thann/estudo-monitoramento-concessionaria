namespace Concessionaria.Relatorios.Aplicacao.Features.Indicadores.Contratos;

/// <summary>Indicadores calculados de um conjunto de vendas.</summary>
/// <param name="QuantidadeVendas">Quantidade de vendas.</param>
/// <param name="Receita">Soma dos valores.</param>
/// <param name="TicketMedio">Receita ÷ quantidade, arredondada em 2 casas (0 sem vendas).</param>
/// <param name="MaiorVenda">Maior valor vendido (0 sem vendas).</param>
/// <param name="MarcaMaisVendida">Marca com mais vendas (empate decidido pela receita); nula sem vendas.</param>
public record IndicadoresVendas(int QuantidadeVendas, decimal Receita, decimal TicketMedio, decimal MaiorVenda, string? MarcaMaisVendida);
