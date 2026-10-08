namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Indicadores de vendas calculados por Relatórios e repassados por Carros/Motos no resumo.</summary>
public record IndicadoresVendas(int QuantidadeVendas, decimal Receita, decimal TicketMedio, decimal MaiorVenda, string? MarcaMaisVendida);
