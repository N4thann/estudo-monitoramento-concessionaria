using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Motos;

/// <summary>GET /api/motos/relatorios/vendas?de=&amp;ate=: repassa a planilha .xlsx das vendas de motos.</summary>
public class GerarPlanilhaVendasMotosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/motos/relatorios/vendas", async ([AsParameters] FiltroPeriodo filtro, IMotosApi api, CancellationToken ct) =>
                await (await api.GerarPlanilhaVendasAsync(filtro, ct)).ParaArquivoAsync(ct))
            .WithTags("Motos")
            .WithName("GerarPlanilhaVendasMotos");
    }
}
