using Refit;

namespace Concessionaria.Bff.Integracoes;

public interface ICarrosApi
{
    [Get("/carros")]
    Task<IApiResponse<ResultadoPaginado<Carro>>> ListarAsync([Query] FiltroVeiculos filtro, CancellationToken ct);

    [Get("/carros/resumo")]
    Task<IApiResponse<Resumo>> ObterResumoAsync(CancellationToken ct);

    [Get("/carros/{id}")]
    Task<IApiResponse<Carro>> ObterPorIdAsync(string id, CancellationToken ct);

    [Post("/carros")]
    Task<IApiResponse<Carro>> CriarAsync([Body] RequisicaoCarro requisicao, CancellationToken ct);

    [Put("/carros/{id}")]
    Task<IApiResponse<Carro>> AtualizarAsync(string id, [Body] RequisicaoCarro requisicao, CancellationToken ct);

    [Delete("/carros/{id}")]
    Task<IApiResponse> ExcluirAsync(string id, CancellationToken ct);

    [Get("/vendas")]
    Task<IApiResponse<List<Venda>>> ListarVendasAsync([Query] FiltroPeriodo filtro, CancellationToken ct);

    [Post("/vendas")]
    Task<IApiResponse<Venda>> RegistrarVendaAsync([Body] RequisicaoVenda requisicao, CancellationToken ct);

    [Get("/relatorios/vendas")]
    Task<IApiResponse<Stream>> GerarPlanilhaVendasAsync([Query] FiltroPeriodo filtro, CancellationToken ct);

    [Get("/relatorios/estoque")]
    Task<IApiResponse<Stream>> GerarPlanilhaEstoqueAsync(CancellationToken ct);
}
