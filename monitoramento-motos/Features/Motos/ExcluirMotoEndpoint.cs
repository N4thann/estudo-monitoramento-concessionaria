using Carter;
using Concessionaria.Motos.Aplicacao.Features.Motos;
using Concessionaria.Motos.Comum;

namespace Concessionaria.Motos.Features.Motos;

/// <summary>DELETE /motos/{id}: exclui uma moto disponível (204, 404 inexistente, 409 já vendida).</summary>
public class ExcluirMotoEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/motos/{id}", async (string id, ServicoMotos servico, CancellationToken ct) =>
                (await servico.ExcluirAsync(id, ct)).Match(_ => Results.NoContent(), erros => erros.ParaRespostaHttp()))
            .WithTags("Motos")
            .WithName("ExcluirMoto");
    }
}
