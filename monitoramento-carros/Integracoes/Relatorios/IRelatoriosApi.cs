using Refit;

namespace Concessionaria.Carros.Integracoes.Relatorios;

public interface IRelatoriosApi
{
    [Post("/relatorios/indicadores/vendas")]
    Task<IApiResponse<IndicadoresVendas>> CalcularIndicadoresVendasAsync([Body] SolicitacaoIndicadoresVendas solicitacao, CancellationToken ct);

    [Post("/relatorios/planilhas/vendas")]
    Task<IApiResponse<Stream>> GerarPlanilhaVendasAsync([Body] SolicitacaoPlanilhaVendas solicitacao, CancellationToken ct);

    [Post("/relatorios/planilhas/estoque")]
    Task<IApiResponse<Stream>> GerarPlanilhaEstoqueAsync([Body] SolicitacaoPlanilhaEstoque solicitacao, CancellationToken ct);
}
