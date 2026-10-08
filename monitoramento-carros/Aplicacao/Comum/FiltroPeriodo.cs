namespace Concessionaria.Carros.Aplicacao.Comum;

/// <summary>Período por data (query string <c>de</c>/<c>ate</c>, formato yyyy-MM-dd), com os dois extremos inclusivos.</summary>
/// <param name="De">Primeiro dia do período; sem limite se nulo.</param>
/// <param name="Ate">Último dia do período; sem limite se nulo.</param>
public record FiltroPeriodo(DateOnly? De, DateOnly? Ate)
{
    /// <summary>Início do primeiro dia, em UTC.</summary>
    public DateTime? ObterInicioUtc() => De?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    /// <summary>
    /// Início do dia seguinte ao último, em UTC, para usar como limite exclusivo: "até" é inclusivo para quem
    /// chama e considera as vendas até o fim daquele dia.
    /// </summary>
    public DateTime? ObterFimExclusivoUtc() => Ate?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
