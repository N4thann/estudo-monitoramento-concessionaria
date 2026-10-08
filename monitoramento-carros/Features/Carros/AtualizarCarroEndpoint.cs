using Carter;
using Concessionaria.Carros.Aplicacao.Features.Carros;
using Concessionaria.Carros.Aplicacao.Features.Carros.Contratos;
using Concessionaria.Carros.Comum;

namespace Concessionaria.Carros.Features.Carros;

/// <summary>PUT /carros/{id}: atualiza um carro disponível (400 inválido, 404 inexistente, 409 já vendido).</summary>
public class AtualizarCarroEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/carros/{id}", async (string id, RequisicaoCarro requisicao, ServicoCarros servico, CancellationToken ct) =>
                (await servico.AtualizarAsync(id, requisicao, ct)).Match(Results.Ok, erros => erros.ParaRespostaHttp()))
            .WithTags("Carros")
            .WithName("AtualizarCarro");
    }
}
