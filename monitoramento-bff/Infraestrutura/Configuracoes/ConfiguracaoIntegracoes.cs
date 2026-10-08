namespace Concessionaria.Bff.Infraestrutura.Configuracoes;

/// <summary>Seção <c>Integracoes</c> do appsettings: endereços e timeouts de Carros e Motos.</summary>
public class ConfiguracaoIntegracoes
{
    /// <summary>Nome da seção no appsettings.</summary>
    public const string Secao = "Integracoes";

    /// <summary>URL base da API de Carros.</summary>
    public string CarrosApi { get; set; } = string.Empty;

    /// <summary>URL base da API de Motos.</summary>
    public string MotosApi { get; set; } = string.Empty;

    /// <summary>
    /// Timeout de cada tentativa, em segundos. Precisa ser maior que o timeout total de Carros/Motos → Relatórios,
    /// senão o BFF desiste enquanto o serviço de baixo ainda está tentando.
    /// </summary>
    public int TimeoutTentativaSegundos { get; set; } = 10;

    /// <summary>Timeout total, somando os retries, em segundos.</summary>
    public int TimeoutTotalSegundos { get; set; } = 15;
}
