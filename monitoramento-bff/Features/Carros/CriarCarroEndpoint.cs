using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Carros;

/// <summary>POST /api/carros: repassa o cadastro de um carro e devolve 201 com o endereço no BFF.</summary>
public class CriarCarroEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/carros", async (RequisicaoCarro requisicao, ICarrosApi api, CancellationToken ct) =>
                (await api.CriarAsync(requisicao, ct)).ParaResultado(carro => Results.Created($"/api/carros/{carro.Id}", carro)))
            .WithTags("Carros")
            .WithName("CriarCarro");
    }
}
