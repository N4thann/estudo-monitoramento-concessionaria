using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;

namespace Concessionaria.Bff.Features.Motos;

/// <summary>DELETE /api/motos/{id}: repassa a exclusão de uma moto (204 se deu certo).</summary>
public class ExcluirMotoEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/motos/{id}", async (string id, IMotosApi api, CancellationToken ct) =>
                (await api.ExcluirAsync(id, ct)).ParaResultadoSemConteudo())
            .WithTags("Motos")
            .WithName("ExcluirMoto");
    }
}
