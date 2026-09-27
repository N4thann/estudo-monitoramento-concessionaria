using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Integracoes;

namespace Concessionaria.Bff.Modulos;

public class ModuloCarros : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/carros").WithTags("Carros");

        grupo.MapGet("/", async ([AsParameters] FiltroVeiculos filtro, ICarrosApi api, CancellationToken ct) =>
            (await api.ListarAsync(filtro, ct)).ParaResultado());

        grupo.MapGet("/{id}", async (string id, ICarrosApi api, CancellationToken ct) =>
            (await api.ObterPorIdAsync(id, ct)).ParaResultado());

        grupo.MapPost("/", async (RequisicaoCarro requisicao, ICarrosApi api, CancellationToken ct) =>
            (await api.CriarAsync(requisicao, ct)).ParaResultado(carro => Results.Created($"/api/carros/{carro.Id}", carro)));

        grupo.MapPut("/{id}", async (string id, RequisicaoCarro requisicao, ICarrosApi api, CancellationToken ct) =>
            (await api.AtualizarAsync(id, requisicao, ct)).ParaResultado());

        grupo.MapDelete("/{id}", async (string id, ICarrosApi api, CancellationToken ct) =>
            (await api.ExcluirAsync(id, ct)).ParaResultadoSemConteudo());

        grupo.MapPost("/vendas", async (RequisicaoVenda requisicao, ICarrosApi api, CancellationToken ct) =>
            (await api.RegistrarVendaAsync(requisicao, ct)).ParaResultado(venda => Results.Created($"/api/vendas/{venda.Id}", venda)));

        grupo.MapGet("/relatorios/vendas", async ([AsParameters] FiltroPeriodo filtro, ICarrosApi api, CancellationToken ct) =>
            await (await api.GerarPlanilhaVendasAsync(filtro, ct)).ParaArquivoAsync(ct));

        grupo.MapGet("/relatorios/estoque", async (ICarrosApi api, CancellationToken ct) =>
            await (await api.GerarPlanilhaEstoqueAsync(ct)).ParaArquivoAsync(ct));
    }
}
