using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Interfaces;

namespace Wakaikami.Core.Hosting;

public sealed partial class DatabaseProbeHostedService(
    DatabaseType database,
    int serverId,
    IDbConnectionFactory connections,
    ILogger<DatabaseProbeHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await connections.ProbeAsync(database, cancellationToken);
        }
        catch (Exception ex)
        {
            // Rethrowing (instead of StopApplication) aborts host startup with a non-zero exit code.
            LogProbeFailed(ex, database, serverId);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Critical,
        Message = "Failed to connect {Database} database. Please check the matching ConnectionStrings entry in appsettings.json (ServerId={ServerId})"
    )]
    private partial void LogProbeFailed(Exception exception, DatabaseType database, int serverId);
}
