using Carter;
using Concessionaria.Motos.Comum;
using Concessionaria.Motos.Servicos;

namespace Concessionaria.Motos.Modulos.Motos;

public class ModuloMotos : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/motos").WithTags("Motos");

        grupo.MapGet("/", async ([AsParameters] FiltroMotos filtro, ServicoMotos servico, CancellationToken ct) =>
            Results.Ok(await servico.ListarAsync(filtro, ct)));

        grupo.MapGet("/resumo", async (ServicoMotos servico, CancellationToken ct) =>
            Results.Ok(await servico.ObterResumoAsync(ct)));

        grupo.MapGet("/{id}", async (string id, ServicoMotos servico, CancellationToken ct) =>
            (await servico.ObterPorIdAsync(id, ct)).Match(Results.Ok, erros => erros.ParaRespostaHttp()));

        grupo.MapPost("/", async (RequisicaoMoto requisicao, ServicoMotos servico, CancellationToken ct) =>
            (await servico.CriarAsync(requisicao, ct)).Match(
                moto => Results.Created($"/motos/{moto.Id}", moto),
                erros => erros.ParaRespostaHttp()));

        grupo.MapPut("/{id}", async (string id, RequisicaoMoto requisicao, ServicoMotos servico, CancellationToken ct) =>
            (await servico.AtualizarAsync(id, requisicao, ct)).Match(Results.Ok, erros => erros.ParaRespostaHttp()));

        grupo.MapDelete("/{id}", async (string id, ServicoMotos servico, CancellationToken ct) =>
            (await servico.ExcluirAsync(id, ct)).Match(_ => Results.NoContent(), erros => erros.ParaRespostaHttp()));
    }
}
