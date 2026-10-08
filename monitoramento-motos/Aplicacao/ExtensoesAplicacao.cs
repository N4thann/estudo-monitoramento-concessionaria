using Concessionaria.Motos.Aplicacao.Features.Motos;
using Concessionaria.Motos.Aplicacao.Features.Relatorios;
using Concessionaria.Motos.Aplicacao.Features.Vendas;

namespace Concessionaria.Motos.Aplicacao;

/// <summary>Registro dos serviços da camada de aplicação.</summary>
public static class ExtensoesAplicacao
{
    /// <summary>Registra os serviços de negócio (um por feature), com escopo por requisição.</summary>
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        services.AddScoped<ServicoMotos>();
        services.AddScoped<ServicoVendas>();
        services.AddScoped<ServicoRelatorios>();

        return services;
    }
}
