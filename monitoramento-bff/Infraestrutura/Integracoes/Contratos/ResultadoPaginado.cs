namespace Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

/// <summary>Uma página de resultados com o total de itens que atendem ao filtro.</summary>
/// <param name="Itens">Itens da página.</param>
/// <param name="Total">Total de itens em todas as páginas.</param>
/// <param name="Pagina">Número da página, começando em 1.</param>
/// <param name="TamanhoPagina">Quantidade máxima de itens por página.</param>
public record ResultadoPaginado<T>(List<T> Itens, long Total, int Pagina, int TamanhoPagina);
