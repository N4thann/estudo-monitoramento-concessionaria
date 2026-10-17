using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Concessionaria.Relatorios.Comum.Observabilidade;

/// <summary>Spans e métricas de negócio próprios deste serviço (os técnicos — HTTP e runtime — vêm da instrumentação).</summary>
public static class Telemetria
{
    /// <summary>Nome da fonte de spans e do medidor de métricas.</summary>
    public const string Nome = "Concessionaria.Relatorios";

    /// <summary>Fonte dos spans próprios.</summary>
    public static readonly ActivitySource Fonte = new(Nome);   // assinada no AddSource(Telemetria.Nome)

    private static readonly Meter Medidor = new(Nome);         // assinado no AddMeter(Telemetria.Nome)

    /// <summary>
    /// Cria o gauge do limite de memória do processo: o do contêiner ou, fora dele, a RAM da máquina. Com ele o uso
    /// de memória vira percentual (alerta MemoriaAlta). É criado na subida, e não como campo, para existir desde o
    /// início sem depender de outra métrica deste medidor ter sido usada antes.
    /// </summary>
    public static void CriarMedidorLimiteMemoria() =>
        Medidor.CreateObservableGauge(
            "concessionaria.processo.memoria_limite",
            LerLimiteMemoria,
            "By",
            "Limite de memória do processo (limite do contêiner ou RAM da máquina).");

    /// <summary>
    /// Lê o cgroup v2 (Docker/WSL2) direto, porque o GC informa só 75% do limite (o teto do heap), e o working set
    /// inclui memória fora do heap. O arquivo contém "max" quando o contêiner não tem limite.
    /// </summary>
    private static long LerLimiteMemoria()
    {
        const string arquivoLimite = "/sys/fs/cgroup/memory.max";
        if (File.Exists(arquivoLimite) && long.TryParse(File.ReadAllText(arquivoLimite).Trim(), out var limite))
            return limite;

        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
    }

    /// <summary>Planilhas geradas, com os rótulos <c>tipo_relatorio</c> e <c>tipo_veiculo</c>.</summary>
    public static readonly Counter<long> PlanilhasGeradas = Medidor.CreateCounter<long>(
        "concessionaria.planilhas.geradas", "{planilha}", "Planilhas geradas, por tipo de relatório e de veículo.");

    /// <summary>
    /// Tempo só da montagem do arquivo (CPU), separado do tempo total da requisição HTTP.
    /// Os limites padrão dos baldes do OpenTelemetry (0, 5, 10, 25… 10000) foram pensados para milissegundos. Em
    /// segundos, toda planilha caía no balde (0, 5] e o p95 calculado dava sempre 4,75 s. Os limites daqui seguem os
    /// que o ASP.NET Core usa para http.server.request.duration.
    /// </summary>
    public static readonly Histogram<double> DuracaoGeracaoPlanilha = Medidor.CreateHistogram(
        "concessionaria.planilhas.duracao", "s", "Tempo para montar a planilha.", tags: null,
        advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10] });

    /// <summary>Quantidade de linhas de dados por planilha gerada.</summary>
    public static readonly Histogram<long> LinhasPorPlanilha = Medidor.CreateHistogram(
        "concessionaria.planilhas.linhas", "{linha}", "Quantidade de linhas de dados por planilha gerada.", tags: null,
        advice: new InstrumentAdvice<long> { HistogramBucketBoundaries = [0, 1, 5, 10, 25, 50, 100, 250, 500, 1000, 5000, 10000] });

    /// <summary>Cálculos de indicadores de vendas atendidos.</summary>
    public static readonly Counter<long> IndicadoresCalculados = Medidor.CreateCounter<long>(
        "concessionaria.indicadores.calculados", "{calculo}", "Cálculos de indicadores de vendas atendidos.");
}
