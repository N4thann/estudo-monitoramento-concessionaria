using Carter;
using Concessionaria.Bff.Aplicacao.Features.Vendas;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Vendas;

/// <summary>GET /api/vendas?de=&amp;ate=: vendas de carros e motos do período numa lista só.</summary>
public class ListarVendasEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/vendas", ([AsParameters] FiltroPeriodo filtro, ServicoVendas servico, CancellationToken ct) =>
                servico.ListarAsync(filtro, ct))
            .WithTags("Vendas")
            .WithName("ListarVendas");
    }
}
