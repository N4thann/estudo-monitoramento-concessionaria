using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Carros;

/// <summary>GET /api/carros: repassa a listagem paginada de Carros.</summary>
public class ListarCarrosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/carros", async ([AsParameters] FiltroVeiculos filtro, ICarrosApi api, CancellationToken ct) =>
                (await api.ListarAsync(filtro, ct)).ParaResultado())
            .WithTags("Carros")
            .WithName("ListarCarros");
    }
}
