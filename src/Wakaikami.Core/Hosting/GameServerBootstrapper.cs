using Microsoft.Extensions.Logging;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Core.Hosting;

public static partial class GameServerBootstrapper
{
    public static async Task<bool> EnsureStartedAsync(
        IServerLifecycle lifecycle,
        Func<bool> startListening,
        ILogger logger,
        string serverName,
        CancellationToken cancellationToken
    )
    {
        if (!lifecycle.LoadedDataClass && !lifecycle.LoadingDataService())
        {
            LogDataServiceFailed(logger, serverName);
            lifecycle.CloseServer();
            return false;
        }

        if (!lifecycle.LoadedGameServer)
        {
            if (!await lifecycle.LoadGameServerModulesAsync(cancellationToken))
            {
                LogGameModulesFailed(logger, serverName);
                lifecycle.CloseServer();
                return false;
            }

            if (!startListening())
            {
                LogListenFailed(logger, serverName);
                lifecycle.CloseServer();
                return false;
            }
        }

        LogServerReady(logger, serverName);
        return true;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "{Server} data service failed to load (gRPC connection lifecycle)")]
    private static partial void LogDataServiceFailed(ILogger logger, string server);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "{Server} game-server modules failed to load (gRPC connection lifecycle)")]
    private static partial void LogGameModulesFailed(ILogger logger, string server);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "{Server} game server failed to start listening (gRPC connection lifecycle)")]
    private static partial void LogListenFailed(ILogger logger, string server);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "{Server} Server Ready (gRPC connection lifecycle)")]
    private static partial void LogServerReady(ILogger logger, string server);
}
