using Concessionaria.Bff.Aplicacao.Features.Painel.Contratos;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Comum.Observabilidade;
using Concessionaria.Bff.Infraestrutura.Integracoes;
using Concessionaria.Bff.Infraestrutura.Integracoes.Contratos;

namespace Concessionaria.Bff.Aplicacao.Features.Painel;

/// <summary>Agregação do painel: busca o resumo de Carros e de Motos em paralelo e soma os dois.</summary>
public class ServicoPainel(ICarrosApi carrosApi, IMotosApi motosApi)
{
    /// <summary>
    /// Devolve 200 com o painel. Se Carros ou Motos falhar, devolve a falha dele (ver <see cref="ExtensoesRespostaApi.ParaFalha"/>).
    /// Se algum resumo vier sem indicadores, o total fica sem vendas e receita e a métrica de painel degradado é contada.
    /// </summary>
    public async Task<IResult> ObterAsync(CancellationToken ct)
    {
        var resumoCarrosTask = carrosApi.ObterResumoAsync(ct);
        var resumoMotosTask = motosApi.ObterResumoAsync(ct);
        await Task.WhenAll(resumoCarrosTask, resumoMotosTask);

        var respostaCarros = resumoCarrosTask.Result;
        var respostaMotos = resumoMotosTask.Result;

        if (!respostaCarros.IsSuccessStatusCode)
            return ExtensoesRespostaApi.ParaFalha(respostaCarros);
        if (!respostaMotos.IsSuccessStatusCode)
            return ExtensoesRespostaApi.ParaFalha(respostaMotos);

        var carros = respostaCarros.Content!;
        var motos = respostaMotos.Content!;
        var temIndicadores = carros.Indicadores is not null && motos.Indicadores is not null;
        if (!temIndicadores)
            Telemetria.PainelDegradado.Add(1);

        return Results.Ok(new RespostaPainel(carros, motos, Somar(carros, motos, temIndicadores)));
    }

    /// <summary>Soma os dois resumos; vendas e receita só entram quando os dois têm indicadores.</summary>
    private static ResumoGeral Somar(Resumo carros, Resumo motos, bool temIndicadores) =>
        new(
            TotalVeiculos: carros.TotalVeiculos + motos.TotalVeiculos,
            Disponiveis: carros.Disponiveis + motos.Disponiveis,
            Vendidos: carros.Vendidos + motos.Vendidos,
            ValorEmEstoque: carros.ValorEmEstoque + motos.ValorEmEstoque,
            QuantidadeVendas: temIndicadores ? carros.Indicadores!.QuantidadeVendas + motos.Indicadores!.QuantidadeVendas : null,
            Receita: temIndicadores ? carros.Indicadores!.Receita + motos.Indicadores!.Receita : null);
}
