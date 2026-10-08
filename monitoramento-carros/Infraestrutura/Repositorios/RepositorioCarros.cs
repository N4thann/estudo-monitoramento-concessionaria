using System.Text.RegularExpressions;
using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;
using Concessionaria.Carros.Dominio.Interfaces;
using Concessionaria.Carros.Dominio.Modelos;
using Concessionaria.Carros.Infraestrutura.Banco;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Concessionaria.Carros.Infraestrutura.Repositorios;

/// <summary>Implementação de <see cref="IRepositorioCarros"/> com o driver do MongoDB.</summary>
public class RepositorioCarros(ContextoMongoDb contexto) : IRepositorioCarros
{
    private readonly IMongoCollection<Carro> _carros = contexto.Carros;

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Carro> Itens, long Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina, SituacaoVeiculo? situacao, string? busca, CancellationToken ct)
    {
        var construtor = Builders<Carro>.Filter;
        var filtro = construtor.Empty;

        if (situacao.HasValue)
            filtro &= construtor.Eq(c => c.Situacao, situacao.Value);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var regex = new BsonRegularExpression(Regex.Escape(busca.Trim()), "i");
            filtro &= construtor.Or(construtor.Regex(c => c.Marca, regex), construtor.Regex(c => c.Modelo, regex));
        }

        var totalTask = _carros.CountDocumentsAsync(filtro, cancellationToken: ct);
        var itensTask = _carros.Find(filtro)
            .SortByDescending(c => c.CriadoEm)
            .Skip((pagina - 1) * tamanhoPagina)
            .Limit(tamanhoPagina)
            .ToListAsync(ct);

        await Task.WhenAll(totalTask, itensTask);
        return (itensTask.Result, totalTask.Result);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Carro>> ListarPorSituacaoAsync(SituacaoVeiculo situacao, CancellationToken ct) =>
        await _carros.Find(c => c.Situacao == situacao).SortByDescending(c => c.CriadoEm).ToListAsync(ct);

    /// <inheritdoc />
    public async Task<Carro?> ObterPorIdAsync(string id, CancellationToken ct) =>
        await _carros.Find(c => c.Id == id).FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    public Task InserirAsync(Carro carro, CancellationToken ct) =>
        _carros.InsertOneAsync(carro, cancellationToken: ct);

    /// <inheritdoc />
    public Task SubstituirAsync(Carro carro, CancellationToken ct) =>
        _carros.ReplaceOneAsync(c => c.Id == carro.Id, carro, cancellationToken: ct);

    /// <inheritdoc />
    public Task ExcluirAsync(string id, CancellationToken ct) =>
        _carros.DeleteOneAsync(c => c.Id == id, ct);

    /// <summary>
    /// Filtra por <c>Situacao = Disponivel</c> no próprio update: duas vendas simultâneas do mesmo carro não passam as duas.
    /// </summary>
    public async Task<bool> TentarMarcarComoVendidoAsync(string id, CancellationToken ct)
    {
        var resultado = await _carros.UpdateOneAsync(
            c => c.Id == id && c.Situacao == SituacaoVeiculo.Disponivel,
            Builders<Carro>.Update
                .Set(c => c.Situacao, SituacaoVeiculo.Vendido)
                .Set(c => c.AtualizadoEm, DateTime.UtcNow),
            cancellationToken: ct);

        return resultado.ModifiedCount == 1;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<SituacaoVeiculo, TotaisEstoque>> ObterTotaisPorSituacaoAsync(CancellationToken ct)
    {
        var grupos = await _carros.Aggregate()
            .Group(c => c.Situacao, g => new { Situacao = g.Key, Quantidade = g.LongCount(), ValorTotal = g.Sum(c => c.Preco) })
            .ToListAsync(ct);

        return grupos.ToDictionary(g => g.Situacao, g => new TotaisEstoque(g.Quantidade, g.ValorTotal));
    }
}
