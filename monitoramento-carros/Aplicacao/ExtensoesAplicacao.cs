using Concessionaria.Carros.Aplicacao.Features.Carros;
using Concessionaria.Carros.Aplicacao.Features.Relatorios;
using Concessionaria.Carros.Aplicacao.Features.Vendas;

namespace Concessionaria.Carros.Aplicacao;

/// <summary>Registro dos serviços da camada de aplicação.</summary>
public static class ExtensoesAplicacao
{
    /// <summary>Registra os serviços de negócio (um por feature), com escopo por requisição.</summary>
    public static IServiceCollection AdicionarAplicacao(this IServiceCollection services)
    {
        services.AddScoped<ServicoCarros>();
        services.AddScoped<ServicoVendas>();
        services.AddScoped<ServicoRelatorios>();

        return services;
    }
}
