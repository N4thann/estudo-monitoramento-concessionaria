using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Concessionaria.Bff.Comum.Observabilidade;

// Spans e métricas próprios do BFF (os técnicos — HTTP, resiliência e runtime — vêm da instrumentação).
public static class Telemetria
{
    public const string Nome = "Concessionaria.Bff";

    public static readonly ActivitySource Fonte = new(Nome);
    private static readonly Meter Medidor = new(Nome);

    // Limite de memória do processo: o do contêiner ou, fora dele, a RAM da máquina. Com ele o uso de memória vira
    // percentual (alerta MemoriaAlta). É criada por AdicionarTelemetria na subida, e não como campo, para existir desde
    // o início sem depender de outra métrica deste medidor ter sido usada antes.
    public static void CriarMedidorLimiteMemoria() =>
        Medidor.CreateObservableGauge(
            "concessionaria.processo.memoria_limite",
            LerLimiteMemoria,
            "By",
            "Limite de memória do processo (limite do contêiner ou RAM da máquina).");

    // Lê o cgroup v2 (Docker/WSL2) direto, porque o GC informa só 75% do limite (o teto do heap), e o working set
    // inclui memória fora do heap. O arquivo contém "max" quando o contêiner não tem limite.
    private static long LerLimiteMemoria()
    {
        const string arquivoLimite = "/sys/fs/cgroup/memory.max";
        if (File.Exists(arquivoLimite) && long.TryParse(File.ReadAllText(arquivoLimite).Trim(), out var limite))
            return limite;

        return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
    }

    // Painel entregue sem indicadores de vendas: sinal de que Relatórios está com problema, visto pela ponta.
    public static readonly Counter<long> PainelDegradado = Medidor.CreateCounter<long>(
        "concessionaria.painel.degradado", "{resposta}", "Respostas do painel sem indicadores de vendas.");
}
