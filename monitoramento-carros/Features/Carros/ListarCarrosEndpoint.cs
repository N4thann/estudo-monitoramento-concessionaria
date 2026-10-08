using Carter;
using Concessionaria.Carros.Aplicacao.Features.Carros;
using Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;

namespace Concessionaria.Carros.Features.Carros;

/// <summary>GET /carros: lista os carros paginados, com filtro por situação e busca por marca ou modelo.</summary>
public class ListarCarrosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/carros", async ([AsParameters] FiltroCarros filtro, ServicoCarros servico, CancellationToken ct) =>
                Results.Ok(await servico.ListarAsync(filtro, ct)))
            .WithTags("Carros")
            .WithName("ListarCarros");
    }
}
