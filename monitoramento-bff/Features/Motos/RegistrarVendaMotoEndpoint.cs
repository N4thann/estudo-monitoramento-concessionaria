using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Motos;

/// <summary>POST /api/motos/vendas: repassa a venda de uma moto (sem retry, para não vender duas vezes).</summary>
public class RegistrarVendaMotoEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/motos/vendas", async (RequisicaoVenda requisicao, IMotosApi api, CancellationToken ct) =>
                (await api.RegistrarVendaAsync(requisicao, ct)).ParaResultado(venda => Results.Created($"/api/vendas/{venda.Id}", venda)))
            .WithTags("Motos")
            .WithName("RegistrarVendaMoto");
    }
}
