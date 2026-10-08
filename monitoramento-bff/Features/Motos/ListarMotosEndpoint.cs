using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Motos;

/// <summary>GET /api/motos: repassa a listagem paginada de Motos.</summary>
public class ListarMotosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/motos", async ([AsParameters] FiltroVeiculos filtro, IMotosApi api, CancellationToken ct) =>
                (await api.ListarAsync(filtro, ct)).ParaResultado())
            .WithTags("Motos")
            .WithName("ListarMotos");
    }
}
