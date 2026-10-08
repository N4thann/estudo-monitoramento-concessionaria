using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Carros;

/// <summary>POST /api/carros/vendas: repassa a venda de um carro (sem retry, para não vender duas vezes).</summary>
public class RegistrarVendaCarroEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/carros/vendas", async (RequisicaoVenda requisicao, ICarrosApi api, CancellationToken ct) =>
                (await api.RegistrarVendaAsync(requisicao, ct)).ParaResultado(venda => Results.Created($"/api/vendas/{venda.Id}", venda)))
            .WithTags("Carros")
            .WithName("RegistrarVendaCarro");
    }
}
