using Concessionaria.Carros.Dominio.Enums;

namespace Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;

/// <summary>Filtros da listagem de carros (query string).</summary>
/// <param name="Pagina">Número da página; padrão 1.</param>
/// <param name="TamanhoPagina">Itens por página; padrão 10, máximo 100.</param>
/// <param name="Situacao">Situação procurada; todas se nula.</param>
/// <param name="Busca">Trecho procurado em marca ou modelo.</param>
public record FiltroCarros(int? Pagina, int? TamanhoPagina, SituacaoVeiculo? Situacao, string? Busca);
