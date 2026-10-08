using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;
using Refit;

namespace Concessionaria.Bff.Infraestrutura.Integracoes;

/// <summary>
/// Cliente Refit da API de Motos, com o mesmo contrato de <see cref="ICarrosApi"/>.
/// <see cref="IApiResponse{T}"/> captura também falhas de rede (StatusCode nulo).
/// </summary>
public interface IMotosApi
{
    /// <summary>Lista as motos paginadas.</summary>
    [Get("/motos")]
    Task<IApiResponse<ResultadoPaginado<Moto>>> ListarAsync([Query] FiltroVeiculos filtro, CancellationToken ct);

    /// <summary>Resumo do estoque e das vendas de motos.</summary>
    [Get("/motos/resumo")]
    Task<IApiResponse<Resumo>> ObterResumoAsync(CancellationToken ct);

    /// <summary>Uma moto pelo Id.</summary>
    [Get("/motos/{id}")]
    Task<IApiResponse<Moto>> ObterPorIdAsync(string id, CancellationToken ct);

    /// <summary>Cadastra uma moto.</summary>
    [Post("/motos")]
    Task<IApiResponse<Moto>> CriarAsync([Body] RequisicaoMoto requisicao, CancellationToken ct);

    /// <summary>Atualiza uma moto.</summary>
    [Put("/motos/{id}")]
    Task<IApiResponse<Moto>> AtualizarAsync(string id, [Body] RequisicaoMoto requisicao, CancellationToken ct);

    /// <summary>Exclui uma moto.</summary>
    [Delete("/motos/{id}")]
    Task<IApiResponse> ExcluirAsync(string id, CancellationToken ct);

    /// <summary>Vendas de motos do período.</summary>
    [Get("/vendas")]
    Task<IApiResponse<List<Venda>>> ListarVendasAsync([Query] FiltroPeriodo filtro, CancellationToken ct);

    /// <summary>Registra a venda de uma moto.</summary>
    [Post("/vendas")]
    Task<IApiResponse<Venda>> RegistrarVendaAsync([Body] RequisicaoVenda requisicao, CancellationToken ct);

    /// <summary>Planilha .xlsx das vendas de motos do período.</summary>
    [Get("/relatorios/vendas")]
    Task<IApiResponse<Stream>> GerarPlanilhaVendasAsync([Query] FiltroPeriodo filtro, CancellationToken ct);

    /// <summary>Planilha .xlsx das motos disponíveis.</summary>
    [Get("/relatorios/estoque")]
    Task<IApiResponse<Stream>> GerarPlanilhaEstoqueAsync(CancellationToken ct);
}
