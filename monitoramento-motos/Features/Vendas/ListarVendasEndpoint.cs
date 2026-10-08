using Carter;
using Concessionaria.Motos.Aplicacao.Comum;
using Concessionaria.Motos.Aplicacao.Features.Vendas;

namespace Concessionaria.Motos.Features.Vendas;

/// <summary>GET /vendas?de=&amp;ate=: vendas de motos do período, da mais recente para a mais antiga.</summary>
public class ListarVendasEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/vendas", async ([AsParameters] FiltroPeriodo filtro, ServicoVendas servico, CancellationToken ct) =>
                Results.Ok(await servico.ListarAsync(filtro, ct)))
            .WithTags("Vendas")
            .WithName("ListarVendas");
    }
}
