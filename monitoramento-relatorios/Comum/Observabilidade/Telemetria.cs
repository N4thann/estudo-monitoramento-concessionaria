using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Concessionaria.Relatorios.Comum.Observabilidade;

// Spans e métricas de negócio próprios deste serviço (os técnicos — HTTP e runtime — vêm da instrumentação).
public static class Telemetria
{
    public const string Nome = "Concessionaria.Relatorios";

    public static readonly ActivitySource Fonte = new(Nome);
    private static readonly Meter Medidor = new(Nome);

    public static readonly Counter<long> PlanilhasGeradas = Medidor.CreateCounter<long>(
        "concessionaria.planilhas.geradas", "{planilha}", "Planilhas geradas, por tipo de relatório e de veículo.");

    // Tempo só da montagem do arquivo (CPU), separado do tempo total da requisição HTTP.
    public static readonly Histogram<double> DuracaoGeracaoPlanilha = Medidor.CreateHistogram<double>(
        "concessionaria.planilhas.duracao", "s", "Tempo para montar a planilha.");

    public static readonly Histogram<long> LinhasPorPlanilha = Medidor.CreateHistogram<long>(
        "concessionaria.planilhas.linhas", "{linha}", "Quantidade de linhas de dados por planilha gerada.");

    public static readonly Counter<long> IndicadoresCalculados = Medidor.CreateCounter<long>(
        "concessionaria.indicadores.calculados", "{calculo}", "Cálculos de indicadores de vendas atendidos.");
}
