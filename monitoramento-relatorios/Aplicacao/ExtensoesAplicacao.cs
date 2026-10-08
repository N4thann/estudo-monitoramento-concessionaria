using Concessionaria.Relatorios.Aplicacao.Features.Indicadores;
using Concessionaria.Relatorios.Aplicacao.Features.Planilhas;

namespace Concessionaria.Relatorios.Aplicacao;

/// <summary>Registro dos serviços da camada de aplicação.</summary>
public static class ExtensoesAplicacao
{
    /// <summary>Registra a calculadora e o gerador de planilhas como singletons: não guardam estado entre requisições.</summary>
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        services.AddSingleton<CalculadoraIndicadores>();
        services.AddSingleton<GeradorPlanilhas>();

        return services;
    }
}
