using Refit;

namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Período por data, repassado na query string no formato yyyy-MM-dd.</summary>
/// <param name="De">Primeiro dia do período; sem limite se nulo.</param>
/// <param name="Ate">Último dia do período; sem limite se nulo.</param>
public record FiltroPeriodo(
    [property: Query(Format = "yyyy-MM-dd")] DateOnly? De,
    [property: Query(Format = "yyyy-MM-dd")] DateOnly? Ate);
