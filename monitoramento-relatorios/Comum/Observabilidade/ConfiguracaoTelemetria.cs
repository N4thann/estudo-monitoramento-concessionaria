namespace Concessionaria.Relatorios.Comum.Observabilidade;

/// <summary>Seção <c>Telemetria</c> do appsettings.</summary>
public class ConfiguracaoTelemetria
{
    /// <summary>Nome da seção no appsettings.</summary>
    public const string Secao = "Telemetria";

    /// <summary>
    /// OpenTelemetry Collector (OTLP/gRPC). Enquanto ele não estiver no ar, o envio falha em silêncio e a API segue normal.
    /// </summary>
    public string EndpointOtlp { get; set; } = "http://127.0.0.1:4317";

    /// <summary>Mostra os spans no console, para vê-los sem nenhuma ferramenta rodando; desligado por padrão porque polui o console.</summary>
    public bool ExportarTracesNoConsole { get; set; }
}
