using Carter;
using Concessionaria.Bff.Comum;
using Concessionaria.Bff.Comum.Observabilidade;
using Concessionaria.Bff.Integracoes;

namespace Concessionaria.Bff.Modulos;

// Quantidade de vendas e receita ficam nulas no total quando algum serviço veio sem indicadores (Relatórios fora).
public record ResumoGeral(long TotalVeiculos, long Disponiveis, long Vendidos, decimal ValorEmEstoque, int? QuantidadeVendas, decimal? Receita);

public record Painel(Resumo Carros, Resumo Motos, ResumoGeral Total);

public class ModuloPainel : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/painel", async (ICarrosApi carrosApi, IMotosApi motosApi, CancellationToken ct) =>
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

            var total = new ResumoGeral(
                TotalVeiculos: carros.TotalVeiculos + motos.TotalVeiculos,
                Disponiveis: carros.Disponiveis + motos.Disponiveis,
                Vendidos: carros.Vendidos + motos.Vendidos,
                ValorEmEstoque: carros.ValorEmEstoque + motos.ValorEmEstoque,
                QuantidadeVendas: temIndicadores ? carros.Indicadores!.QuantidadeVendas + motos.Indicadores!.QuantidadeVendas : null,
                Receita: temIndicadores ? carros.Indicadores!.Receita + motos.Indicadores!.Receita : null);

            return Results.Ok(new Painel(carros, motos, total));
        }).WithTags("Painel");
    }
}
