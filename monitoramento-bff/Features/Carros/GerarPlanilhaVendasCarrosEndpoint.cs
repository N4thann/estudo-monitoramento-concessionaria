using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Carros;

/// <summary>GET /api/carros/relatorios/vendas?de=&amp;ate=: repassa a planilha .xlsx das vendas de carros.</summary>
public class GerarPlanilhaVendasCarrosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/carros/relatorios/vendas", async ([AsParameters] FiltroPeriodo filtro, ICarrosApi api, CancellationToken ct) =>
                await (await api.GerarPlanilhaVendasAsync(filtro, ct)).ParaArquivoAsync(ct))
            .WithTags("Carros")
            .WithName("GerarPlanilhaVendasCarros");
    }
}
