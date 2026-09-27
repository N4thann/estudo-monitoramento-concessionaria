using Carter;
using Concessionaria.Carros.Comum;
using Concessionaria.Carros.Servicos;

namespace Concessionaria.Carros.Modulos.Carros;

public class ModuloCarros : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/carros").WithTags("Carros");

        grupo.MapGet("/", async ([AsParameters] FiltroCarros filtro, ServicoCarros servico, CancellationToken ct) =>
            Results.Ok(await servico.ListarAsync(filtro, ct)));

        grupo.MapGet("/resumo", async (ServicoCarros servico, CancellationToken ct) =>
            Results.Ok(await servico.ObterResumoAsync(ct)));

        grupo.MapGet("/{id}", async (string id, ServicoCarros servico, CancellationToken ct) =>
            (await servico.ObterPorIdAsync(id, ct)).Match(Results.Ok, erros => erros.ParaRespostaHttp()));

        grupo.MapPost("/", async (RequisicaoCarro requisicao, ServicoCarros servico, CancellationToken ct) =>
            (await servico.CriarAsync(requisicao, ct)).Match(
                carro => Results.Created($"/carros/{carro.Id}", carro),
                erros => erros.ParaRespostaHttp()));

        grupo.MapPut("/{id}", async (string id, RequisicaoCarro requisicao, ServicoCarros servico, CancellationToken ct) =>
            (await servico.AtualizarAsync(id, requisicao, ct)).Match(Results.Ok, erros => erros.ParaRespostaHttp()));

        grupo.MapDelete("/{id}", async (string id, ServicoCarros servico, CancellationToken ct) =>
            (await servico.ExcluirAsync(id, ct)).Match(_ => Results.NoContent(), erros => erros.ParaRespostaHttp()));
    }
}
