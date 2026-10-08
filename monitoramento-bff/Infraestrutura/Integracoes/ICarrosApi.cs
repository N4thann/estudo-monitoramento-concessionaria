using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Refit;

namespace Concessionaria.Bff.Infraestrutura.Integracoes;

/// <summary>
/// Cliente Refit da API de Carros. <see cref="IApiResponse{T}"/> captura também falhas de rede
/// (StatusCode nulo e a causa em <c>Error.InnerException</c>), então nada aqui lança por erro HTTP.
/// </summary>
public interface ICarrosApi
{
    /// <summary>Lista os carros paginados.</summary>
    [Get("/carros")]
    Task<IApiResponse<ResultadoPaginado<Carro>>> ListarAsync([Query] FiltroVeiculos filtro, CancellationToken ct);

    /// <summary>Resumo do estoque e das vendas de carros.</summary>
    [Get("/carros/resumo")]
    Task<IApiResponse<Resumo>> ObterResumoAsync(CancellationToken ct);

    /// <summary>Um carro pelo Id.</summary>
    [Get("/carros/{id}")]
    Task<IApiResponse<Carro>> ObterPorIdAsync(string id, CancellationToken ct);

    /// <summary>Cadastra um carro.</summary>
    [Post("/carros")]
    Task<IApiResponse<Carro>> CriarAsync([Body] RequisicaoCarro requisicao, CancellationToken ct);

    /// <summary>Atualiza um carro.</summary>
    [Put("/carros/{id}")]
    Task<IApiResponse<Carro>> AtualizarAsync(string id, [Body] RequisicaoCarro requisicao, CancellationToken ct);

    /// <summary>Exclui um carro.</summary>
    [Delete("/carros/{id}")]
    Task<IApiResponse> ExcluirAsync(string id, CancellationToken ct);

    /// <summary>Vendas de carros do período.</summary>
    [Get("/vendas")]
    Task<IApiResponse<List<Venda>>> ListarVendasAsync([Query] FiltroPeriodo filtro, CancellationToken ct);

    /// <summary>Registra a venda de um carro.</summary>
    [Post("/vendas")]
    Task<IApiResponse<Venda>> RegistrarVendaAsync([Body] RequisicaoVenda requisicao, CancellationToken ct);

    /// <summary>Planilha .xlsx das vendas de carros do período.</summary>
    [Get("/relatorios/vendas")]
    Task<IApiResponse<Stream>> GerarPlanilhaVendasAsync([Query] FiltroPeriodo filtro, CancellationToken ct);

    /// <summary>Planilha .xlsx dos carros disponíveis.</summary>
    [Get("/relatorios/estoque")]
    Task<IApiResponse<Stream>> GerarPlanilhaEstoqueAsync(CancellationToken ct);
}
