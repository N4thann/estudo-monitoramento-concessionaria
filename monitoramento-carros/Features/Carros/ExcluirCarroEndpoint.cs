using Carter;
using Concessionaria.Carros.Aplicacao.Features.Carros;
using Concessionaria.Carros.Comum;

namespace Concessionaria.Carros.Features.Carros;

/// <summary>DELETE /carros/{id}: exclui um carro disponível (204, 404 inexistente, 409 já vendido).</summary>
public class ExcluirCarroEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/carros/{id}", async (string id, ServicoCarros servico, CancellationToken ct) =>
                (await servico.ExcluirAsync(id, ct)).Match(_ => Results.NoContent(), erros => erros.ParaRespostaHttp()))
            .WithTags("Carros")
            .WithName("ExcluirCarro");
    }
}
