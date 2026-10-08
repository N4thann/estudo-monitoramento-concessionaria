using Carter;
using Concessionaria.Motos.Aplicacao.Features.Motos;

namespace Concessionaria.Motos.Features.Motos;

/// <summary>GET /motos/resumo: totais do estoque e indicadores de vendas (nulos se Relatórios estiver fora).</summary>
public class ObterResumoMotosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/motos/resumo", async (ServicoMotos servico, CancellationToken ct) =>
                Results.Ok(await servico.ObterResumoAsync(ct)))
            .WithTags("Motos")
            .WithName("ObterResumoMotos");
    }
}
