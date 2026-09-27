using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;

namespace Concessionaria.Motos.Dados;

public class VerificacaoSaudeMongoDb(ContextoMongoDb contexto) : IHealthCheck
{
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
