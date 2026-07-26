using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Core.Hosting.Health;

public sealed class LifecycleHealthCheck(IServerLifecycle lifecycle) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!lifecycle.LoadedDataClass)
            return Task.FromResult(HealthCheckResult.Unhealthy("Data classes not loaded yet"));

        if (!lifecycle.LoadedGameServer)
            return Task.FromResult(HealthCheckResult.Unhealthy("Game server modules not booted yet"));

        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
