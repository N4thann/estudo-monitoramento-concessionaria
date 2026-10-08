using Carter;
using Concessionaria.Carros.Aplicacao.Features.Carros;
using Concessionaria.Carros.Comum;

namespace Concessionaria.Carros.Features.Carros;

/// <summary>GET /carros/{id}: um carro pelo Id (404 se não existir).</summary>
public class ObterCarroPorIdEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/carros/{id}", async (string id, ServicoCarros servico, CancellationToken ct) =>
                (await servico.ObterPorIdAsync(id, ct)).Match(Results.Ok, erros => erros.ParaRespostaHttp()))
            .WithTags("Carros")
            .WithName("ObterCarroPorId");
    }
}
