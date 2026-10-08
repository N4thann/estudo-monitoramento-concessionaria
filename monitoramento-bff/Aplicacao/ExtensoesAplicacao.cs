using Concessionaria.Bff.Aplicacao.Features.Painel;
using Concessionaria.Bff.Aplicacao.Features.Vendas;

namespace Concessionaria.Bff.Aplicacao;

/// <summary>Registro dos serviços da camada de aplicação.</summary>
public static class ExtensoesAplicacao
{
    /// <summary>
    /// Registra os serviços de agregação (painel e vendas). As rotas de carros e motos não têm serviço: só repassam
    /// a chamada ao cliente Refit.
    /// </summary>
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        services.AddScoped<ServicoPainel>();
        services.AddScoped<ServicoVendas>();

        return services;
    }
}
