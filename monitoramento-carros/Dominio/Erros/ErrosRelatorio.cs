using ErrorOr;

namespace Concessionaria.Carros.Dominio.Erros;

/// <summary>Erros da geração de planilhas pelo serviço de Relatórios.</summary>
public static class ErrosRelatorio
{
    /// <summary>Maior intervalo, em dias, aceito na planilha de vendas.</summary>
    public const int PeriodoMaximoDias = 366;

    /// <summary>Período com início depois do fim ou maior que <see cref="PeriodoMaximoDias"/> (400).</summary>
    public static readonly Error PeriodoInvalido = Error.Validation(
        "Periodo", $"Período inválido: a data inicial deve ser anterior à final e o intervalo deve ter até {PeriodoMaximoDias} dias.");

    /// <summary>O serviço de Relatórios respondeu com erro ou não respondeu (502).</summary>
    public static readonly Error FalhaNaGeracao = Error.Failure(
        "Relatorios.FalhaNaGeracao", "Não foi possível gerar a planilha no serviço de relatórios.");
}
