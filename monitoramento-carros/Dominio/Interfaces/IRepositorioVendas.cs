using Concessionaria.Carros.Dominio.Entidades;

namespace Concessionaria.Carros.Dominio.Interfaces;

/// <summary>Acesso à coleção de vendas.</summary>
public interface IRepositorioVendas
{
    /// <summary>Vendas do período, da mais recente para a mais antiga.</summary>
    /// <param name="de">Início inclusivo (UTC); sem limite se nulo.</param>
    /// <param name="ate">Fim exclusivo (UTC); sem limite se nulo.</param>
    /// <param name="ct">Token de cancelamento.</param>
    Task<IReadOnlyList<Venda>> ListarPorPeriodoAsync(DateTime? de, DateTime? ate, CancellationToken ct);

    /// <summary>Grava uma venda nova.</summary>
    Task InserirAsync(Venda venda, CancellationToken ct);
}
