using Concessionaria.Bff.Aplicacao.Features.Vendas.Contratos;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Dominio.Enums;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Aplicacao.Features.Vendas;

/// <summary>Lista unificada de vendas: junta as vendas de Carros e de Motos do mesmo período.</summary>
public class ServicoVendas(ICarrosApi carrosApi, IMotosApi motosApi)
{
    /// <summary>
    /// Busca as vendas dos dois serviços em paralelo e devolve 200 com uma lista só, da mais recente para a mais
    /// antiga. Se Carros ou Motos falhar, devolve a falha dele (ver <see cref="ExtensoesRespostaApi.ParaFalha"/>).
    /// </summary>
    public async Task<IResult> ListarAsync(FiltroPeriodo filtro, CancellationToken ct)
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
    }
}
