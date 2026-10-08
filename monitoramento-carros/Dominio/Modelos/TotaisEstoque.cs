namespace Concessionaria.Carros.Dominio.Modelos;

/// <summary>Quantidade e soma dos preços dos carros de uma mesma situação, calculadas no banco.</summary>
/// <param name="Quantidade">Quantidade de carros.</param>
/// <param name="ValorTotal">Soma dos preços.</param>
public record TotaisEstoque(long Quantidade, decimal ValorTotal);
