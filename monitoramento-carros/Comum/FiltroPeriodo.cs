namespace Concessionaria.Carros.Comum;

public record FiltroPeriodo(DateOnly? De, DateOnly? Ate)
{
    public DateTime? ObterInicioUtc() => De?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    // "Até" é inclusivo para quem chama: considera as vendas até o fim daquele dia.
    public DateTime? ObterFimExclusivoUtc() => Ate?.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
