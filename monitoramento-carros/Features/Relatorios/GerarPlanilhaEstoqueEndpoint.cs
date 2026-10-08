using Carter;
using Concessionaria.Carros.Aplicacao.Features.Relatorios;
using Concessionaria.Carros.Aplicacao.Features.Relatorios.Contratos;
using Concessionaria.Carros.Comum;

namespace Concessionaria.Carros.Features.Relatorios;

/// <summary>GET /relatorios/estoque: planilha .xlsx dos carros disponíveis (502 se Relatórios falhar).</summary>
public class GerarPlanilhaEstoqueEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/relatorios/estoque", async (ServicoRelatorios servico, CancellationToken ct) =>
                (await servico.GerarPlanilhaEstoqueAsync(ct)).Match(
                    arquivo => Results.File(arquivo.Conteudo, ArquivoRelatorio.TipoConteudoXlsx, arquivo.NomeArquivo),
                    erros => erros.ParaRespostaHttp()))
            .WithTags("Relatórios")
            .WithName("GerarPlanilhaEstoque");
    }
}
