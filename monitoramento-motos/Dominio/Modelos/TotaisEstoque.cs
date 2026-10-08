namespace Concessionaria.Motos.Dominio.Modelos;

/// <summary>Quantidade e soma dos preços das motos de uma mesma situação, calculadas no banco.</summary>
/// <param name="Quantidade">Quantidade de motos.</param>
/// <param name="ValorTotal">Soma dos preços.</param>
public record TotaisEstoque(long Quantidade, decimal ValorTotal);
