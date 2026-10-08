using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;

namespace Concessionaria.Bff.Features.Motos;

/// <summary>GET /api/motos/relatorios/estoque: repassa a planilha .xlsx das motos disponíveis.</summary>
public class GerarPlanilhaEstoqueMotosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/motos/relatorios/estoque", async (IMotosApi api, CancellationToken ct) =>
                await (await api.GerarPlanilhaEstoqueAsync(ct)).ParaArquivoAsync(ct))
            .WithTags("Motos")
            .WithName("GerarPlanilhaEstoqueMotos");
    }
}
