using Refit;

namespace Concessionaria.Bff.Integracoes;

public interface IMotosApi
{
    [Get("/motos")]
    Task<IApiResponse<ResultadoPaginado<Moto>>> ListarAsync([Query] FiltroVeiculos filtro, CancellationToken ct);

    [Get("/motos/resumo")]
    Task<IApiResponse<Resumo>> ObterResumoAsync(CancellationToken ct);

    [Get("/motos/{id}")]
    Task<IApiResponse<Moto>> ObterPorIdAsync(string id, CancellationToken ct);

    [Post("/motos")]
    Task<IApiResponse<Moto>> CriarAsync([Body] RequisicaoMoto requisicao, CancellationToken ct);

    [Put("/motos/{id}")]
    Task<IApiResponse<Moto>> AtualizarAsync(string id, [Body] RequisicaoMoto requisicao, CancellationToken ct);

    [Delete("/motos/{id}")]
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
