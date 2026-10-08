using Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios.Contratos;
using Refit;

namespace Concessionaria.Motos.Infraestrutura.Integracoes.Relatorios;

/// <summary>
/// Cliente Refit da API de Relatórios. Ela não tem banco: recebe os dados de quem pede e devolve indicadores
/// calculados ou a planilha gerada. <see cref="IApiResponse{T}"/> captura também falhas de rede (StatusCode nulo).
/// </summary>
public interface IRelatoriosApi
{
    /// <summary>Calcula os indicadores das vendas enviadas.</summary>
    [Post("/relatorios/indicadores/vendas")]
    Task<IApiResponse<IndicadoresVendas>> CalcularIndicadoresVendasAsync([Body] SolicitacaoIndicadoresVendas solicitacao, CancellationToken ct);

    /// <summary>Gera a planilha .xlsx das vendas enviadas.</summary>
    [Post("/relatorios/planilhas/vendas")]
    Task<IApiResponse<Stream>> GerarPlanilhaVendasAsync([Body] SolicitacaoPlanilhaVendas solicitacao, CancellationToken ct);

    /// <summary>Gera a planilha .xlsx do estoque enviado.</summary>
    [Post("/relatorios/planilhas/estoque")]
    Task<IApiResponse<Stream>> GerarPlanilhaEstoqueAsync([Body] SolicitacaoPlanilhaEstoque solicitacao, CancellationToken ct);
}
