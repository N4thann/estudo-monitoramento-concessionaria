using Concessionaria.Carros.Dominio.Entidades;
using Concessionaria.Carros.Dominio.Enums;
using Concessionaria.Carros.Dominio.Modelos;

namespace Concessionaria.Carros.Dominio.Interfaces;

/// <summary>Acesso à coleção de carros.</summary>
public interface IRepositorioCarros
{
    /// <summary>Página de carros, do mais recente para o mais antigo, com o total que atende ao filtro.</summary>
    /// <param name="pagina">Número da página, começando em 1.</param>
    /// <param name="tamanhoPagina">Quantidade de itens por página.</param>
    /// <param name="situacao">Filtra pela situação, quando informada.</param>
    /// <param name="busca">Trecho procurado em marca ou modelo, sem diferenciar maiúsculas.</param>
    /// <param name="ct">Token de cancelamento.</param>
    Task<(IReadOnlyList<Carro> Itens, long Total)> ListarPaginadoAsync(int pagina, int tamanhoPagina, SituacaoVeiculo? situacao, string? busca, CancellationToken ct);

    /// <summary>Todos os carros de uma situação, do mais recente para o mais antigo.</summary>
    Task<IReadOnlyList<Carro>> ListarPorSituacaoAsync(SituacaoVeiculo situacao, CancellationToken ct);

    /// <summary>Busca um carro pelo Id; nulo se não existir.</summary>
    Task<Carro?> ObterPorIdAsync(string id, CancellationToken ct);

    /// <summary>Insere um carro novo.</summary>
    Task InserirAsync(Carro carro, CancellationToken ct);

    /// <summary>Substitui o documento do carro com o mesmo Id.</summary>
    Task SubstituirAsync(Carro carro, CancellationToken ct);

    /// <summary>Remove o carro com o Id informado.</summary>
    Task ExcluirAsync(string id, CancellationToken ct);

    /// <summary>
    /// Marca o carro como vendido de forma atômica, só se ele ainda estiver disponível.
    /// Devolve falso se o carro já estava vendido (ou não existe).
    /// </summary>
    Task<bool> TentarMarcarComoVendidoAsync(string id, CancellationToken ct);

    /// <summary>Quantidade e valor total dos carros agrupados por situação, calculados no banco.</summary>
    Task<IReadOnlyDictionary<SituacaoVeiculo, TotaisEstoque>> ObterTotaisPorSituacaoAsync(CancellationToken ct);
}
