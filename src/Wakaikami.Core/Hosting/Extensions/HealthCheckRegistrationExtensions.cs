using Microsoft.Extensions.DependencyInjection;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Interfaces;
using Wakaikami.Core.Hosting.Health;

namespace Wakaikami.Core.Hosting.Extensions;

public static class HealthCheckRegistrationExtensions
{
    /// <summary>Tag for checks that gate the readiness endpoint (<c>/ready</c>).</summary>
    public const string ReadyTag = "ready";

    /// <summary>
    /// Readiness checks shared by the game services: server lifecycle fully booted and the
    /// service's database reachable.
    /// </summary>
    public static IHealthChecksBuilder AddGameServerReadiness(this IHealthChecksBuilder builder, DatabaseType databaseType)
    {
        builder.Services.AddSingleton(sp => new DatabaseHealthCheck(sp.GetRequiredService<IDbConnectionFactory>(), databaseType));

        return builder
            .AddCheck<LifecycleHealthCheck>("lifecycle", tags: [ReadyTag])
            .AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag]);
    }
}
