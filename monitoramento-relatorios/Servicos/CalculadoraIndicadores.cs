using Concessionaria.Relatorios.Comum.Observabilidade;
using Concessionaria.Relatorios.Contratos;

namespace Concessionaria.Relatorios.Servicos;

public class CalculadoraIndicadores(ILogger<CalculadoraIndicadores> logger)
{
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
