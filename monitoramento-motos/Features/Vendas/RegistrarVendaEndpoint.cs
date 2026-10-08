using Carter;
using Concessionaria.Motos.Aplicacao.Features.Vendas;
using Concessionaria.Motos.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Motos.Comum;

namespace Concessionaria.Motos.Features.Vendas;

/// <summary>POST /vendas: registra a venda de uma moto (201, 400 inválida, 404 inexistente, 409 já vendida).</summary>
public class RegistrarVendaEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/vendas", async (RequisicaoVenda requisicao, ServicoVendas servico, CancellationToken ct) =>
                (await servico.RegistrarAsync(requisicao, ct)).Match(
                    venda => Results.Created($"/vendas/{venda.Id}", venda),
                    erros => erros.ParaRespostaHttp()))
            .WithTags("Vendas")
            .WithName("RegistrarVenda");
    }
}
