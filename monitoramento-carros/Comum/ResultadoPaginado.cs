namespace Concessionaria.Carros.Comum;

public record ResultadoPaginado<T>(IReadOnlyList<T> Itens, long Total, int Pagina, int TamanhoPagina);
