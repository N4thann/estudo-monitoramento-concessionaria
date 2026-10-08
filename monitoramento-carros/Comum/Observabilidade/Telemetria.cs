using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Concessionaria.Carros.Comum.Observabilidade;

/// <summary>Spans e métricas de negócio próprios deste serviço (os técnicos — HTTP, Mongo, runtime — vêm da instrumentação).</summary>
public static class Telemetria
{
    /// <summary>Nome da fonte de spans e do medidor de métricas.</summary>
    public const string Nome = "Concessionaria.Carros";

    /// <summary>Valor do rótulo <c>tipo_veiculo</c> nas métricas deste serviço.</summary>
    public const string TipoVeiculo = "carro";

    /// <summary>Fonte dos spans próprios.</summary>
    public static readonly ActivitySource Fonte = new(Nome);

    private static readonly Meter Medidor = new(Nome);

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

    /// <summary>Vendas registradas com sucesso.</summary>
    public static readonly Counter<long> VendasRegistradas = Medidor.CreateCounter<long>(
        "concessionaria.vendas.registradas", "{venda}", "Vendas registradas com sucesso.");

    /// <summary>Soma do valor das vendas registradas, em reais.</summary>
    public static readonly Counter<double> ValorVendido = Medidor.CreateCounter<double>(
        "concessionaria.vendas.valor", "BRL", "Soma do valor das vendas registradas.");

    /// <summary>Tentativas de venda recusadas, com o rótulo <c>motivo</c>.</summary>
    public static readonly Counter<long> VendasRecusadas = Medidor.CreateCounter<long>(
        "concessionaria.vendas.recusadas", "{venda}", "Tentativas de venda recusadas, por motivo.");

    /// <summary>Chamadas a Relatórios que falharam, com o rótulo <c>operacao</c>.</summary>
    public static readonly Counter<long> FalhasRelatorios = Medidor.CreateCounter<long>(
        "concessionaria.relatorios.falhas_integracao", "{falha}", "Chamadas ao serviço de Relatórios que falharam, por operação.");

    /// <summary>Rótulo <c>tipo_veiculo</c> presente em todas as métricas de negócio deste serviço.</summary>
    public static KeyValuePair<string, object?> TagTipoVeiculo => new("tipo_veiculo", TipoVeiculo);
}
