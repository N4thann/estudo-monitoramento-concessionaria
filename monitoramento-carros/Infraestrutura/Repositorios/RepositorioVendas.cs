using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Interfaces;
using Concessionaria.Carros.Infraestrutura.Banco;
using MongoDB.Driver;

namespace Concessionaria.Carros.Infraestrutura.Repositorios;

/// <summary>Implementação de <see cref="IRepositorioVendas"/> com o driver do MongoDB.</summary>
public class RepositorioVendas(ContextoMongoDb contexto) : IRepositorioVendas
{
    private readonly IMongoCollection<Venda> _vendas = contexto.Vendas;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Venda>> ListarPorPeriodoAsync(DateTime? de, DateTime? ate, CancellationToken ct)
    {
        var construtor = Builders<Venda>.Filter;
        var filtro = construtor.Empty;

        if (de.HasValue)
            filtro &= construtor.Gte(v => v.VendidaEm, de.Value);

        if (ate.HasValue)
            filtro &= construtor.Lt(v => v.VendidaEm, ate.Value);

        return await _vendas.Find(filtro).SortByDescending(v => v.VendidaEm).ToListAsync(ct);
    }

    /// <inheritdoc />
    public Task InserirAsync(Venda venda, CancellationToken ct) =>
        _vendas.InsertOneAsync(venda, cancellationToken: ct);
}
