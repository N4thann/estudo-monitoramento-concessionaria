using Carter;
using Concessionaria.Motos.Aplicacao.Features.Motos;
using Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;
using Concessionaria.Motos.Comum;

namespace Concessionaria.Motos.Features.Motos;

/// <summary>POST /motos: cadastra uma moto (201 com Location, ou 400 com os erros por campo).</summary>
public class CriarMotoEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/motos", async (RequisicaoMoto requisicao, ServicoMotos servico, CancellationToken ct) =>
                (await servico.CriarAsync(requisicao, ct)).Match(
                    moto => Results.Created($"/motos/{moto.Id}", moto),
                    erros => erros.ParaRespostaHttp()))
            .WithTags("Motos")
            .WithName("CriarMoto");
    }
}
