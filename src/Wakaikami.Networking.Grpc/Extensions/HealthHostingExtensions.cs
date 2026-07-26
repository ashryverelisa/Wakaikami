using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Wakaikami.Core.Hosting.Extensions;

namespace Wakaikami.Networking.Grpc.Extensions;

public static class HealthHostingExtensions
{
    /// <summary>
    /// Maps <c>/healthz</c> (liveness: process responsive, no checks) and <c>/ready</c>
    /// (readiness: all checks tagged <see cref="HealthCheckRegistrationExtensions.ReadyTag"/>).
    /// Served on the same plaintext listener as <c>/metrics</c>.
    /// </summary>
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = static _ => false });
        app.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = static r => r.Tags.Contains(HealthCheckRegistrationExtensions.ReadyTag) });
        return app;
    }
}
