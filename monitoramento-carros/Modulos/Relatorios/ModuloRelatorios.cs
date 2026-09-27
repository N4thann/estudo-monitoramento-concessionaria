using Carter;
using Concessionaria.Carros.Comum;
using Concessionaria.Carros.Servicos;

namespace Concessionaria.Carros.Modulos.Relatorios;

public class ModuloRelatorios : ICarterModule
{
    private const string TipoConteudoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/relatorios").WithTags("Relatórios");

        grupo.MapGet("/vendas", async ([AsParameters] FiltroPeriodo filtro, ServicoRelatorios servico, CancellationToken ct) =>
            (await servico.GerarPlanilhaVendasAsync(filtro, ct)).Match(
                arquivo => Results.File(arquivo.Conteudo, TipoConteudoXlsx, arquivo.NomeArquivo),
                erros => erros.ParaRespostaHttp()));

        grupo.MapGet("/estoque", async (ServicoRelatorios servico, CancellationToken ct) =>
            (await servico.GerarPlanilhaEstoqueAsync(ct)).Match(
                arquivo => Results.File(arquivo.Conteudo, TipoConteudoXlsx, arquivo.NomeArquivo),
                erros => erros.ParaRespostaHttp()));
    }
}
