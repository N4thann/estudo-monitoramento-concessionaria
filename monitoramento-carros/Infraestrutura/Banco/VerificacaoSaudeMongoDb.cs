using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;

namespace Concessionaria.Carros.Infraestrutura.Banco;

/// <summary>Health check de <c>/saude</c>: a API só está saudável se o Mongo responde ao ping.</summary>
public class VerificacaoSaudeMongoDb(ContextoMongoDb contexto) : IHealthCheck
{
    /// <summary>Envia um <c>ping</c> ao banco; qualquer falha deixa a API como indisponível.</summary>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await contexto.Banco.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MongoDB indisponível.", ex);
        }
    }
}
