using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Motos;

/// <summary>POST /api/motos: repassa o cadastro de uma moto e devolve 201 com o endereço no BFF.</summary>
public class CriarMotoEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/motos", async (RequisicaoMoto requisicao, IMotosApi api, CancellationToken ct) =>
                (await api.CriarAsync(requisicao, ct)).ParaResultado(moto => Results.Created($"/api/motos/{moto.Id}", moto)))
            .WithTags("Motos")
            .WithName("CriarMoto");
    }
}
