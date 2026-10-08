using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Features.Carros;

/// <summary>PUT /api/carros/{id}: repassa a atualização de um carro.</summary>
public class AtualizarCarroEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/carros/{id}", async (string id, RequisicaoCarro requisicao, ICarrosApi api, CancellationToken ct) =>
                (await api.AtualizarAsync(id, requisicao, ct)).ParaResultado())
            .WithTags("Carros")
            .WithName("AtualizarCarro");
    }
}
