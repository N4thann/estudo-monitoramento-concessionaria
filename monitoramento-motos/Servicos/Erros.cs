using ErrorOr;

namespace Concessionaria.Motos.Servicos;

public static class ErrosMoto
{
    public static readonly Error NaoEncontrado = Error.NotFound("Moto.NaoEncontrado", "Moto não encontrada.");
    public static readonly Error JaVendido = Error.Conflict("Moto.JaVendido", "Moto já foi vendida.");
}

public static class ErrosRelatorio
{
    public const int PeriodoMaximoDias = 366;

    public static readonly Error PeriodoInvalido = Error.Validation(
        "Periodo", $"Período inválido: a data inicial deve ser anterior à final e o intervalo deve ter até {PeriodoMaximoDias} dias.");

    public static readonly Error FalhaNaGeracao = Error.Failure(
        "Relatorios.FalhaNaGeracao", "Não foi possível gerar a planilha no serviço de relatórios.");
}
