using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Motos;

/// <summary>PUT /api/motos/{id}: repassa a atualização de uma moto.</summary>
public class AtualizarMotoEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/motos/{id}", async (string id, RequisicaoMoto requisicao, IMotosApi api, CancellationToken ct) =>
                (await api.AtualizarAsync(id, requisicao, ct)).ParaResultado())
            .WithTags("Motos")
            .WithName("AtualizarMoto");
    }
}
