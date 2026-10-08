using Carter;
using Concessionaria.Carros.Aplicacao.Features.Vendas;
using Concessionaria.Carros.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Carros.Comum;

namespace Concessionaria.Carros.Features.Vendas;

/// <summary>POST /vendas: registra a venda de um carro (201, 400 inválida, 404 inexistente, 409 já vendido).</summary>
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
