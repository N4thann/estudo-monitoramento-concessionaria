using Carter;
using Concessionaria.Motos.Aplicacao.Features.Motos;
using Concessionaria.Motos.Comum;

namespace Concessionaria.Motos.Features.Motos;

/// <summary>GET /motos/{id}: uma moto pelo Id (404 se não existir).</summary>
public class ObterMotoPorIdEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/motos/{id}", async (string id, ServicoMotos servico, CancellationToken ct) =>
                (await servico.ObterPorIdAsync(id, ct)).Match(Results.Ok, erros => erros.ParaRespostaHttp()))
            .WithTags("Motos")
            .WithName("ObterMotoPorId");
    }
}
