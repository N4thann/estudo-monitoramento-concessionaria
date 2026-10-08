using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;

namespace Concessionaria.Bff.Features.Carros;

/// <summary>GET /api/carros/relatorios/estoque: repassa a planilha .xlsx dos carros disponíveis.</summary>
public class GerarPlanilhaEstoqueCarrosEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/carros/relatorios/estoque", async (ICarrosApi api, CancellationToken ct) =>
                await (await api.GerarPlanilhaEstoqueAsync(ct)).ParaArquivoAsync(ct))
            .WithTags("Carros")
            .WithName("GerarPlanilhaEstoqueCarros");
    }
}
