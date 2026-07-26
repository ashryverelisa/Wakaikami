using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Interfaces;

namespace Wakaikami.Core.Hosting.Health;

public sealed class DatabaseHealthCheck(IDbConnectionFactory connections, DatabaseType databaseType) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await connections.ProbeAsync(databaseType, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"{databaseType} database unreachable", ex);
        }
    }
}
