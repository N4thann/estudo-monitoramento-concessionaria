namespace Concessionaria.Carros.Integracoes;

public class ConfiguracaoIntegracoes
{
    public const string Secao = "Integracoes";

    public string RelatoriosApi { get; set; } = string.Empty;
    public int TimeoutTentativaSegundos { get; set; } = 3;
    public int TimeoutTotalSegundos { get; set; } = 8;
}
