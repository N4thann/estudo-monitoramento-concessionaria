using Concessionaria.Motos.Dominio.Entidades;
using Concessionaria.Motos.Dominio.Enums;
using Concessionaria.Motos.Dominio.Modelos;

namespace Concessionaria.Motos.Dominio.Interfaces;

/// <summary>Acesso à coleção de motos.</summary>
public interface IRepositorioMotos
{
    /// <summary>Página de motos, da mais recente para a mais antiga, com o total que atende ao filtro.</summary>
    /// <param name="pagina">Número da página, começando em 1.</param>
    /// <param name="tamanhoPagina">Quantidade de itens por página.</param>
    /// <param name="situacao">Filtra pela situação, quando informada.</param>
    /// <param name="busca">Trecho procurado em marca ou modelo, sem diferenciar maiúsculas.</param>
    /// <param name="ct">Token de cancelamento.</param>
    Task<(IReadOnlyList<Moto> Itens, long Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina, SituacaoVeiculo? situacao, string? busca, CancellationToken ct);

    /// <summary>Todas as motos de uma situação, da mais recente para a mais antiga.</summary>
    Task<IReadOnlyList<Moto>> ListarPorSituacaoAsync(SituacaoVeiculo situacao, CancellationToken ct);

    /// <summary>Busca uma moto pelo Id; nulo se não existir.</summary>
    Task<Moto?> ObterPorIdAsync(string id, CancellationToken ct);

    /// <summary>Insere uma moto nova.</summary>
    Task InserirAsync(Moto moto, CancellationToken ct);

    /// <summary>Substitui o documento da moto com o mesmo Id.</summary>
    Task SubstituirAsync(Moto moto, CancellationToken ct);

    /// <summary>Remove a moto com o Id informado.</summary>
    Task ExcluirAsync(string id, CancellationToken ct);

    /// <summary>
    /// Marca a moto como vendida de forma atômica, só se ela ainda estiver disponível.
    /// Devolve falso se a moto já estava vendida (ou não existe).
    /// </summary>
    Task<bool> TentarMarcarComoVendidoAsync(string id, CancellationToken ct);

    /// <summary>Quantidade e valor total das motos agrupados por situação, calculados no banco.</summary>
    Task<IReadOnlyDictionary<SituacaoVeiculo, TotaisEstoque>> ObterTotaisPorSituacaoAsync(CancellationToken ct);
}
