using System.Text.RegularExpressions;
using Concessionaria.Motos.Entidades;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Concessionaria.Motos.Dados;

public interface IRepositorioMotos
{
    Task<(IReadOnlyList<Moto> Itens, long Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina, SituacaoVeiculo? situacao, string? busca, CancellationToken ct);
    Task<IReadOnlyList<Moto>> ListarPorSituacaoAsync(SituacaoVeiculo situacao, CancellationToken ct);
    Task<Moto?> ObterPorIdAsync(string id, CancellationToken ct);
    Task InserirAsync(Moto moto, CancellationToken ct);
    Task SubstituirAsync(Moto moto, CancellationToken ct);
    Task ExcluirAsync(string id, CancellationToken ct);
    Task<bool> TentarMarcarComoVendidoAsync(string id, CancellationToken ct);
    Task<IReadOnlyDictionary<SituacaoVeiculo, TotaisEstoque>> ObterTotaisPorSituacaoAsync(CancellationToken ct);
}

public record TotaisEstoque(long Quantidade, decimal ValorTotal);

public class RepositorioMotos(ContextoMongoDb contexto) : IRepositorioMotos
{
    private readonly IMongoCollection<Moto> _motos = contexto.Motos;

    public async Task<(IReadOnlyList<Moto> Itens, long Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina, SituacaoVeiculo? situacao, string? busca, CancellationToken ct)
    {
        var construtor = Builders<Moto>.Filter;
        var filtro = construtor.Empty;

        if (situacao.HasValue)
            filtro &= construtor.Eq(c => c.Situacao, situacao.Value);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var regex = new BsonRegularExpression(Regex.Escape(busca.Trim()), "i");
            filtro &= construtor.Or(construtor.Regex(c => c.Marca, regex), construtor.Regex(c => c.Modelo, regex));
        }

        var totalTask = _motos.CountDocumentsAsync(filtro, cancellationToken: ct);
        var itensTask = _motos.Find(filtro)
            .SortByDescending(c => c.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Limit(tamanhoPagina)
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, itensTask);
        return (itensTask.Result, totalTask.Result);
    }

    public async Task<IReadOnlyList<Moto>> ListarPorSituacaoAsync(SituacaoVeiculo situacao, CancellationToken ct) =>
        await _motos.Find(c => c.Situacao == situacao).SortByDescending(c => c.CriadoEm).ToListAsync(ct);

    public async Task<Moto?> ObterPorIdAsync(string id, CancellationToken ct) =>
        await _motos.Find(c => c.Id == id).FirstOrDefaultAsync(ct);

    public Task InserirAsync(Moto moto, CancellationToken ct) =>
        _motos.InsertOneAsync(moto, cancellationToken: ct);

    public Task SubstituirAsync(Moto moto, CancellationToken ct) =>
        _motos.ReplaceOneAsync(c => c.Id == moto.Id, moto, cancellationToken: ct);

    public Task ExcluirAsync(string id, CancellationToken ct) =>
        _motos.DeleteOneAsync(c => c.Id == id, ct);

    // Filtra por Situacao = Disponivel no próprio update: duas vendas simultâneas da mesma moto não passam as duas.
    public async Task<bool> TentarMarcarComoVendidoAsync(string id, CancellationToken ct)
    {
        var resultado = await _motos.UpdateOneAsync(
            c => c.Id == id && c.Situacao == SituacaoVeiculo.Disponivel,
            Builders<Moto>.Update
                .Set(c => c.Situacao, SituacaoVeiculo.Vendido)
                .Set(c => c.AtualizadoEm, DateTime.UtcNow),
            cancellationToken: ct);

        return resultado.ModifiedCount == 1;
    }

    public async Task<IReadOnlyDictionary<SituacaoVeiculo, TotaisEstoque>> ObterTotaisPorSituacaoAsync(CancellationToken ct)
    {
        var grupos = await _motos.Aggregate()
            .Group(c => c.Situacao, g => new { Situacao = g.Key, Quantidade = g.LongCount(), ValorTotal = g.Sum(c => c.Preco) })
            .ToListAsync(ct);

        return grupos.ToDictionary(g => g.Situacao, g => new TotaisEstoque(g.Quantidade, g.ValorTotal));
    }
}
