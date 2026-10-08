using Carter;
using Concessionaria.Motos.Aplicacao.Features.Motos;
using Concessionaria.Motos.Aplicacao.Features.Motos.Contratos;
using Concessionaria.Motos.Comum;

namespace Concessionaria.Motos.Features.Motos;

/// <summary>PUT /motos/{id}: atualiza uma moto disponível (400 inválida, 404 inexistente, 409 já vendida).</summary>
public class AtualizarMotoEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/motos/{id}", async (string id, RequisicaoMoto requisicao, ServicoMotos servico, CancellationToken ct) =>
                (await servico.AtualizarAsync(id, requisicao, ct)).Match(Results.Ok, erros => erros.ParaRespostaHttp()))
            .WithTags("Motos")
            .WithName("AtualizarMoto");
    }
}
