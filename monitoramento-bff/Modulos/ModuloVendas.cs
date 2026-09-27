using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Integracoes;

namespace Concessionaria.Bff.Modulos;

public enum TipoVeiculo
{
    Carro,
    Moto
}

public record VendaComTipo(
    string Id,
    TipoVeiculo TipoVeiculo,
    string VeiculoId,
    string DescricaoVeiculo,
    string Marca,
    string NomeCliente,
    string DocumentoCliente,
    decimal Valor,
    DateTime VendidaEm)
{
    public static VendaComTipo De(Venda venda, TipoVeiculo tipo) =>
        new(venda.Id, tipo, venda.VeiculoId, venda.DescricaoVeiculo, venda.Marca, venda.NomeCliente, venda.DocumentoCliente, venda.Valor, venda.VendidaEm);
}

public class ModuloVendas : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Junta as vendas dos dois serviços em uma lista única, ordenada por data.
        app.MapGet("/api/vendas", async ([AsParameters] FiltroPeriodo filtro, ICarrosApi carrosApi, IMotosApi motosApi, CancellationToken ct) =>
        {
            var vendasCarrosTask = carrosApi.ListarVendasAsync(filtro, ct);
            var vendasMotosTask = motosApi.ListarVendasAsync(filtro, ct);
            await Task.WhenAll(vendasCarrosTask, vendasMotosTask);

            var vendasCarros = vendasCarrosTask.Result;
            var vendasMotos = vendasMotosTask.Result;

            if (!vendasCarros.IsSuccessStatusCode)
                return ExtensoesRespostaApi.ParaFalha(vendasCarros);
            if (!vendasMotos.IsSuccessStatusCode)
                return ExtensoesRespostaApi.ParaFalha(vendasMotos);

            var vendas = vendasCarros.Content!.Select(v => VendaComTipo.De(v, TipoVeiculo.Carro))
                .Concat(vendasMotos.Content!.Select(v => VendaComTipo.De(v, TipoVeiculo.Moto)))
                .OrderByDescending(v => v.VendidaEm)
                .ToList();

            return Results.Ok(vendas);
        }).WithTags("Vendas");
    }
}
