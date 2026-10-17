using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Concessionaria.Bff.Comum.Observabilidade;

/// <summary>Spans e métricas próprios do BFF (os técnicos — HTTP, resiliência e runtime — vêm da instrumentação).</summary>
public static class Telemetria
{
    /// <summary>Nome da fonte de spans e do medidor de métricas.</summary>
    public const string Nome = "Concessionaria.Bff";

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

    /// <summary>Painel entregue sem indicadores de vendas: sinal de que Relatórios está com problema, visto pela ponta.</summary>
    public static readonly Counter<long> PainelDegradado = Medidor.CreateCounter<long>(
        "concessionaria.painel.degradado", "{resposta}", "Respostas do painel sem indicadores de vendas.");
}
