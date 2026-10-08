using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;

namespace Concessionaria.Bff.Features.Carros;

/// <summary>DELETE /api/carros/{id}: repassa a exclusão de um carro (204 se deu certo).</summary>
public class ExcluirCarroEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/carros/{id}", async (string id, ICarrosApi api, CancellationToken ct) =>
                (await api.ExcluirAsync(id, ct)).ParaResultadoSemConteudo())
            .WithTags("Carros")
            .WithName("ExcluirCarro");
    }
}
