using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Core.Hosting;

public sealed partial class ServerShutdownHostedService(
    IServerLifecycle lifecycle,
    ILogger<ServerShutdownHostedService> logger
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            lifecycle.CloseServer();
        }
        catch (Exception ex)
        {
            LogShutdownFailed(ex);
        }

        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Error during server shutdown.")]
    private partial void LogShutdownFailed(Exception exception);
}
