using Carter;
using Concessionaria.Motos.Aplicacao.Features.Motos;
using Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;

namespace Concessionaria.Motos.Features.Motos;

/// <summary>GET /motos: lista as motos paginadas, com filtro por situação e busca por marca ou modelo.</summary>
public class ListarMotosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/motos", async ([AsParameters] FiltroMotos filtro, ServicoMotos servico, CancellationToken ct) =>
                Results.Ok(await servico.ListarAsync(filtro, ct)))
            .WithTags("Motos")
            .WithName("ListarMotos");
    }
}
