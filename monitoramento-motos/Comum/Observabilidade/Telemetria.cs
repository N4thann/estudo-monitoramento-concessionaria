using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Concessionaria.Motos.Comum.Observabilidade;

// Spans e métricas de negócio próprios deste serviço (os técnicos — HTTP, Mongo, runtime — vêm da instrumentação).
public static class Telemetria
{
    public const string Nome = "Concessionaria.Motos";
    public const string TipoVeiculo = "moto";

    public static readonly ActivitySource Fonte = new(Nome);
    private static readonly Meter Medidor = new(Nome);

    public static readonly Counter<long> VendasRegistradas = Medidor.CreateCounter<long>(
        "concessionaria.vendas.registradas", "{venda}", "Vendas registradas com sucesso.");

    public static readonly Counter<double> ValorVendido = Medidor.CreateCounter<double>(
        "concessionaria.vendas.valor", "BRL", "Soma do valor das vendas registradas.");

    public static readonly Counter<long> VendasRecusadas = Medidor.CreateCounter<long>(
        "concessionaria.vendas.recusadas", "{venda}", "Tentativas de venda recusadas, por motivo.");

    public static readonly Counter<long> FalhasRelatorios = Medidor.CreateCounter<long>(
        "concessionaria.relatorios.falhas_integracao", "{falha}", "Chamadas ao serviço de Relatórios que falharam, por operação.");

    public static KeyValuePair<string, object?> TagTipoVeiculo => new("tipo_veiculo", TipoVeiculo);
}
