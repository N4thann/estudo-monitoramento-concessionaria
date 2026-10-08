using Carter;
using Concessionaria.Carros.Aplicacao.Features.Carros;
using Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;
using Concessionaria.Carros.Comum;

namespace Concessionaria.Carros.Features.Carros;

/// <summary>POST /carros: cadastra um carro (201 com Location, ou 400 com os erros por campo).</summary>
public class CriarCarroEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/carros", async (RequisicaoCarro requisicao, ServicoCarros servico, CancellationToken ct) =>
                (await servico.CriarAsync(requisicao, ct)).Match(
                    carro => Results.Created($"/carros/{carro.Id}", carro),
                    erros => erros.ParaRespostaHttp()))
            .WithTags("Carros")
            .WithName("CriarCarro");
    }
}
