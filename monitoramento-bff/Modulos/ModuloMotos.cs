using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Integracoes;

namespace Concessionaria.Bff.Modulos;

public class ModuloMotos : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/motos").WithTags("Motos");

        grupo.MapGet("/", async ([AsParameters] FiltroVeiculos filtro, IMotosApi api, CancellationToken ct) =>
            (await api.ListarAsync(filtro, ct)).ParaResultado());

        grupo.MapGet("/{id}", async (string id, IMotosApi api, CancellationToken ct) =>
            (await api.ObterPorIdAsync(id, ct)).ParaResultado());

        grupo.MapPost("/", async (RequisicaoMoto requisicao, IMotosApi api, CancellationToken ct) =>
            (await api.CriarAsync(requisicao, ct)).ParaResultado(moto => Results.Created($"/api/motos/{moto.Id}", moto)));

        grupo.MapPut("/{id}", async (string id, RequisicaoMoto requisicao, IMotosApi api, CancellationToken ct) =>
            (await api.AtualizarAsync(id, requisicao, ct)).ParaResultado());

        grupo.MapDelete("/{id}", async (string id, IMotosApi api, CancellationToken ct) =>
            (await api.ExcluirAsync(id, ct)).ParaResultadoSemConteudo());

        grupo.MapPost("/vendas", async (RequisicaoVenda requisicao, IMotosApi api, CancellationToken ct) =>
            (await api.RegistrarVendaAsync(requisicao, ct)).ParaResultado(venda => Results.Created($"/api/vendas/{venda.Id}", venda)));

        grupo.MapGet("/relatorios/vendas", async ([AsParameters] FiltroPeriodo filtro, IMotosApi api, CancellationToken ct) =>
            await (await api.GerarPlanilhaVendasAsync(filtro, ct)).ParaArquivoAsync(ct));

        grupo.MapGet("/relatorios/estoque", async (IMotosApi api, CancellationToken ct) =>
            await (await api.GerarPlanilhaEstoqueAsync(ct)).ParaArquivoAsync(ct));
    }
}
