using Carter;
using Concessionaria.Carros.Comum;
using Concessionaria.Carros.Servicos;

namespace Concessionaria.Carros.Modulos.Vendas;

public class ModuloVendas : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/vendas").WithTags("Vendas");

        grupo.MapGet("/", async ([AsParameters] FiltroPeriodo filtro, ServicoVendas servico, CancellationToken ct) =>
            Results.Ok(await servico.ListarAsync(filtro, ct)));

        grupo.MapPost("/", async (RequisicaoVenda requisicao, ServicoVendas servico, CancellationToken ct) =>
            (await servico.RegistrarAsync(requisicao, ct)).Match(
                venda => Results.Created($"/vendas/{venda.Id}", venda),
                erros => erros.ParaRespostaHttp()));
    }
}
