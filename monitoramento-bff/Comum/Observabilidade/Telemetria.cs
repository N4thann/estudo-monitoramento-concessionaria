using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Concessionaria.Bff.Comum.Observabilidade;

// Spans e métricas próprios do BFF (os técnicos — HTTP, resiliência e runtime — vêm da instrumentação).
public static class Telemetria
{
    public const string Nome = "Concessionaria.Bff";

    public static readonly ActivitySource Fonte = new(Nome);
    private static readonly Meter Medidor = new(Nome);

    // Painel entregue sem indicadores de vendas: sinal de que Relatórios está com problema, visto pela ponta.
    public static readonly Counter<long> PainelDegradado = Medidor.CreateCounter<long>(
        "concessionaria.painel.degradado", "{resposta}", "Respostas do painel sem indicadores de vendas.");
}
