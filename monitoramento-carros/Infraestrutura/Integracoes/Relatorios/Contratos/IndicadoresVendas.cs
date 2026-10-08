namespace Concessionaria.Carros.Infraestrutura.Integracoes.Relatorios.Contratos;

/// <summary>Indicadores de vendas calculados por Relatórios.</summary>
/// <param name="QuantidadeVendas">Quantidade de vendas.</param>
/// <param name="Receita">Soma dos valores.</param>
/// <param name="TicketMedio">Receita ÷ quantidade (0 sem vendas).</param>
/// <param name="MaiorVenda">Maior valor vendido (0 sem vendas).</param>
/// <param name="MarcaMaisVendida">Marca com mais vendas; nula sem vendas.</param>
public record IndicadoresVendas(int QuantidadeVendas, decimal Receita, decimal TicketMedio, decimal MaiorVenda, string? MarcaMaisVendida);
