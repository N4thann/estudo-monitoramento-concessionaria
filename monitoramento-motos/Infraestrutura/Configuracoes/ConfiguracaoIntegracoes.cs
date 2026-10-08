namespace Concessionaria.Motos.Infraestrutura.Configuracoes;

/// <summary>Seção <c>Integracoes</c> do appsettings: endereço e timeouts do serviço de Relatórios.</summary>
public class ConfiguracaoIntegracoes
{
    /// <summary>Nome da seção no appsettings.</summary>
    public const string Secao = "Integracoes";

    /// <summary>URL base da API de Relatórios.</summary>
    public string RelatoriosApi { get; set; } = string.Empty;

    /// <summary>Timeout de cada tentativa, em segundos.</summary>
    public int TimeoutTentativaSegundos { get; set; } = 3;

    /// <summary>Timeout total, somando os retries, em segundos. Precisa ser menor que o timeout por tentativa do BFF.</summary>
    public int TimeoutTotalSegundos { get; set; } = 8;
}
