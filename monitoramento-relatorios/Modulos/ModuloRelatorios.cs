using Carter;
using Concessionaria.Relatorios.Contratos;
using Concessionaria.Relatorios.Servicos;

namespace Concessionaria.Relatorios.Modulos;

public class ModuloRelatorios : ICarterModule
{
    private const string TipoConteudoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string TituloValidacao = "Um ou mais campos são inválidos.";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/relatorios").WithTags("Relatórios");

        grupo.MapPost("/indicadores/vendas", (SolicitacaoIndicadoresVendas solicitacao, CalculadoraIndicadores calculadora) =>
            Results.Ok(calculadora.CalcularVendas(solicitacao.Vendas ?? [])));

        grupo.MapPost("/planilhas/vendas", (SolicitacaoPlanilhaVendas solicitacao, GeradorPlanilhas gerador) =>
        {
            var erros = solicitacao.Validar();
            if (erros.Count > 0)
                return Results.ValidationProblem(erros, title: TituloValidacao);

            var arquivo = gerador.GerarVendas(solicitacao);
            return Results.File(arquivo.Conteudo, TipoConteudoXlsx, arquivo.NomeArquivo);
        });

        grupo.MapPost("/planilhas/estoque", (SolicitacaoPlanilhaEstoque solicitacao, GeradorPlanilhas gerador) =>
        {
            var erros = solicitacao.Validar();
            if (erros.Count > 0)
                return Results.ValidationProblem(erros, title: TituloValidacao);

            var arquivo = gerador.GerarEstoque(solicitacao);
            return Results.File(arquivo.Conteudo, TipoConteudoXlsx, arquivo.NomeArquivo);
        });
    }
}
