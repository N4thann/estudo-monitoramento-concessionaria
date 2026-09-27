using ErrorOr;

namespace Concessionaria.Carros.Servicos;

public static class ErrosCarro
{
    public static readonly Error NaoEncontrado = Error.NotFound("Carro.NaoEncontrado", "Carro não encontrado.");
    public static readonly Error JaVendido = Error.Conflict("Carro.JaVendido", "Carro já foi vendido.");
}

public static class ErrosRelatorio
{
    public const int PeriodoMaximoDias = 366;

    public static readonly Error PeriodoInvalido = Error.Validation(
        "Periodo", $"Período inválido: a data inicial deve ser anterior à final e o intervalo deve ter até {PeriodoMaximoDias} dias.");

    public static readonly Error FalhaNaGeracao = Error.Failure(
        "Relatorios.FalhaNaGeracao", "Não foi possível gerar a planilha no serviço de relatórios.");
}
