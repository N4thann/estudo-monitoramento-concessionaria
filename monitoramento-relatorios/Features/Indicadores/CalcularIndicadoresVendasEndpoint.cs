using Carter;
using Concessionaria.Relatorios.Aplicacao.Features.Indicadores;
using Concessionaria.Relatorios.Aplicacao.Features.Indicadores.Contratos;

namespace Concessionaria.Relatorios.Features.Indicadores;

/// <summary>POST /relatorios/indicadores/vendas: indicadores das vendas enviadas (lista ausente conta como vazia).</summary>
public class CalcularIndicadoresVendasEndpoint : ICarterModule
{
    /// <summary>Mapeia a rota.</summary>
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/relatorios/indicadores/vendas", (SolicitacaoIndicadoresVendas solicitacao, CalculadoraIndicadores calculadora) =>
                Results.Ok(calculadora.CalcularVendas(solicitacao.Vendas ?? [])))
            .WithTags("Indicadores")
            .WithName("CalcularIndicadoresVendas");
    }
}
