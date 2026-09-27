namespace Concessionaria.Bff.Integracoes;

public class ConfiguracaoIntegracoes
{
    public const string Secao = "Integracoes";

    public string CarrosApi { get; set; } = string.Empty;
    public string MotosApi { get; set; } = string.Empty;

    // O timeout por tentativa do BFF precisa ser maior que o timeout total de Carros/Motos → Relatórios,
    // senão o BFF desiste enquanto o serviço de baixo ainda está tentando.
    public int TimeoutTentativaSegundos { get; set; } = 10;
    public int TimeoutTotalSegundos { get; set; } = 15;
}
