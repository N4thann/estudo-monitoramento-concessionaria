using Carter;
using Concessionaria.Motos.Aplicacao.Comum;
using Concessionaria.Motos.Aplicacao.Features.Relatorios;
using Concessionaria.Motos.Aplicacao.Features.Relatorios.Contratos;
using Concessionaria.Motos.Comum;

namespace Concessionaria.Motos.Features.Relatorios;

/// <summary>GET /relatorios/vendas?de=&amp;ate=: planilha .xlsx das vendas (400 período inválido, 502 falha em Relatórios).</summary>
public class GerarPlanilhaVendasEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/relatorios/vendas", async ([AsParameters] FiltroPeriodo filtro, ServicoRelatorios servico, CancellationToken ct) =>
                (await servico.GerarPlanilhaVendasAsync(filtro, ct)).Match(
                    arquivo => Results.File(arquivo.Conteudo, ArquivoRelatorio.TipoConteudoXlsx, arquivo.NomeArquivo),
                    erros => erros.ParaRespostaHttp()))
            .WithTags("Relatórios")
            .WithName("GerarPlanilhaVendas");
    }
}
