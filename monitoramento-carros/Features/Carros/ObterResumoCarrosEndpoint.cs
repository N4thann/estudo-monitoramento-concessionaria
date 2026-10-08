using Carter;
using Concessionaria.Carros.Aplicacao.Features.Carros;

namespace Concessionaria.Carros.Features.Carros;

/// <summary>GET /carros/resumo: totais do estoque e indicadores de vendas (nulos se Relatórios estiver fora).</summary>
public class ObterResumoCarrosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/carros/resumo", async (ServicoCarros servico, CancellationToken ct) =>
                Results.Ok(await servico.ObterResumoAsync(ct)))
            .WithTags("Carros")
            .WithName("ObterResumoCarros");
    }
}
