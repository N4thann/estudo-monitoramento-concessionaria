using Concessionaria.Motos.Entidades;
using MongoDB.Driver;

namespace Concessionaria.Motos.Dados;

public interface IRepositorioVendas
{
    Task<IReadOnlyList<Venda>> ListarPorPeriodoAsync(DateTime? de, DateTime? ate, CancellationToken ct);
    Task InserirAsync(Venda venda, CancellationToken ct);
}

public class RepositorioVendas(ContextoMongoDb contexto) : IRepositorioVendas
{
    private readonly IMongoCollection<Venda> _vendas = contexto.Vendas;

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

    public Task InserirAsync(Venda venda, CancellationToken ct) =>
        _vendas.InsertOneAsync(venda, cancellationToken: ct);
}
