using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;

namespace Concessionaria.Bff.Features.Motos;

/// <summary>GET /api/motos/{id}: repassa a busca de uma moto pelo Id.</summary>
public class ObterMotoPorIdEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/motos/{id}", async (string id, IMotosApi api, CancellationToken ct) =>
                (await api.ObterPorIdAsync(id, ct)).ParaResultado())
            .WithTags("Motos")
            .WithName("ObterMotoPorId");
    }
}
