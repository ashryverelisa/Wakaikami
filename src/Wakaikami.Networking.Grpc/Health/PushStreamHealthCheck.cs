using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Wakaikami.Networking.Grpc.Health;

/// <summary>
/// Degraded (not unhealthy) while the push stream to the upstream service is down: the
/// subscriber reconnects on its own, and flipping readiness would drop the pod from its
/// service endpoints and cut players off for a transient upstream blip.
/// </summary>
public sealed class PushStreamHealthCheck(IPushStreamStatus stream) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            stream.IsStreamConnected ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded("Push stream disconnected; reconnecting")
        );
    }
}
