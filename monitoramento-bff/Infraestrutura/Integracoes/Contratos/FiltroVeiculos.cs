using Concessionaria.Bff.Dominio.Enums;

namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Filtros da listagem de carros ou motos, recebidos na query string e repassados como vieram.</summary>
/// <param name="Pagina">Número da página.</param>
/// <param name="TamanhoPagina">Itens por página.</param>
/// <param name="Situacao">Situação procurada; todas se nula.</param>
/// <param name="Busca">Trecho procurado em marca ou modelo.</param>
public record FiltroVeiculos(int? Pagina, int? TamanhoPagina, SituacaoVeiculo? Situacao, string? Busca);
