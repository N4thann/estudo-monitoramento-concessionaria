using Concessionaria.Relatorios.Aplicacao.Comum.Contratos;
using Concessionaria.Relatorios.Aplicacao.Features.Indicadores.Contratos;
using Concessionaria.Relatorios.Comum.Observabilidade;

namespace Concessionaria.Relatorios.Aplicacao.Features.Indicadores;

/// <summary>Cálculo dos indicadores de vendas (usado pelo resumo do painel e pela aba "Resumo" da planilha de vendas).</summary>
public class CalculadoraIndicadores(ILogger<CalculadoraIndicadores> logger)
{
    /// <summary>Calcula quantidade, receita, ticket médio, maior venda e marca mais vendida.</summary>
    public IndicadoresVendas CalcularVendas(IReadOnlyList<VendaRelatorio> vendas)
    {
        var receita = vendas.Sum(v => v.Valor);

        var marcaMaisVendida = vendas
            .GroupBy(v => v.Marca)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Sum(v => v.Valor))
            .Select(g => g.Key)
            .FirstOrDefault();

        var indicadores = new IndicadoresVendas(
            QuantidadeVendas: vendas.Count,
            Receita: receita,
            TicketMedio: vendas.Count == 0 ? 0m : Math.Round(receita / vendas.Count, 2),
            MaiorVenda: vendas.Count == 0 ? 0m : vendas.Max(v => v.Valor),
            MarcaMaisVendida: marcaMaisVendida);

        logger.LogInformation("Indicadores calculados para {QuantidadeVendas} vendas", indicadores.QuantidadeVendas);
        Telemetria.IndicadoresCalculados.Add(1);
        return indicadores;
    }
}
