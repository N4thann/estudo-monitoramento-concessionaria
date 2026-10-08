using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;

namespace Concessionaria.Bff.Features.Carros;

/// <summary>GET /api/carros/{id}: repassa a busca de um carro pelo Id.</summary>
public class ObterCarroPorIdEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/carros/{id}", async (string id, ICarrosApi api, CancellationToken ct) =>
                (await api.ObterPorIdAsync(id, ct)).ParaResultado())
            .WithTags("Carros")
            .WithName("ObterCarroPorId");
    }
}
