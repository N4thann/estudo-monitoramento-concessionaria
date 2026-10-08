using Carter;
using Concessionaria.Relatorios.Aplicacao.Features.Planilhas;
using Concessionaria.Relatorios.Aplicacao.Features.Planilhas.Contratos;

namespace Concessionaria.Relatorios.Features.Planilhas;

/// <summary>POST /relatorios/planilhas/estoque: planilha .xlsx do estoque enviado (400 com os erros por campo).</summary>
public class GerarPlanilhaEstoqueEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/relatorios/planilhas/estoque", (SolicitacaoPlanilhaEstoque solicitacao, GeradorPlanilhas gerador) =>
            {
                var erros = solicitacao.Validar();
                if (erros.Count > 0)
                    return Results.ValidationProblem(erros, title: "Um ou mais campos são inválidos.");

                var arquivo = gerador.GerarEstoque(solicitacao);
                return Results.File(arquivo.Conteudo, ArquivoPlanilha.TipoConteudoXlsx, arquivo.NomeArquivo);
            })
            .WithTags("Planilhas")
            .WithName("GerarPlanilhaEstoque");
    }
}
