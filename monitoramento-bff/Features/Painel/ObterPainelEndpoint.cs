using Carter;
using Concessionaria.Bff.Aplicacao.Features.Painel;

namespace Concessionaria.Bff.Features.Painel;

/// <summary>GET /api/painel: resumo de Carros e Motos somados (sem vendas e receita se Relatórios estiver fora).</summary>
public class ObterPainelEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/painel", (ServicoPainel servico, CancellationToken ct) => servico.ObterAsync(ct))
            .WithTags("Painel")
            .WithName("ObterPainel");
    }
}
