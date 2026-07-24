using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Interfaces;

namespace Wakaikami.Core.Hosting.Extensions;

public static class DatabaseProbeRegistrationExtensions
{
    public static IServiceCollection AddDatabaseStartupProbe(this IServiceCollection services, DatabaseType database, Func<IServiceProvider, int> serverId) =>
        services.AddHostedService(sp => new DatabaseProbeHostedService(
            database,
            serverId(sp),
            sp.GetRequiredService<IDbConnectionFactory>(),
            sp.GetRequiredService<ILogger<DatabaseProbeHostedService>>()
        ));
}
