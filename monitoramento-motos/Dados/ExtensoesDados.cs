using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;

namespace Concessionaria.Motos.Dados;

public static class ExtensoesDados
{
    public static IServiceCollection AdicionarMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ConfiguracaoMongoDb>(configuration.GetSection(ConfiguracaoMongoDb.Secao));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var configuracoesCliente = MongoClientSettings.FromConnectionString(
                sp.GetRequiredService<IOptions<ConfiguracaoMongoDb>>().Value.StringConexao);

            // O driver já gera spans OpenTelemetry nativamente; aqui só incluímos o texto do comando no span
            // (db.query.text) para identificar consultas lentas. Dados de estudo — em produção avaliar PII antes.
            configuracoesCliente.TracingOptions = new TracingOptions { QueryTextMaxLength = 1000 };

            return new MongoClient(configuracoesCliente);
        });

        services.AddSingleton(sp =>
        {
            var configuracao = sp.GetRequiredService<IOptions<ConfiguracaoMongoDb>>().Value;
            return new ContextoMongoDb(sp.GetRequiredService<IMongoClient>().GetDatabase(configuracao.NomeBanco));
        });

        services.AddScoped<IRepositorioMotos, RepositorioMotos>();
        services.AddScoped<IRepositorioVendas, RepositorioVendas>();
        services.AddHostedService<InicializadorMongoDb>();

        return services;
    }
}
