using Microsoft.Extensions.Hosting;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Core.Hosting;

public sealed class DataLoadingHostedService(IServerLifecycle lifecycle) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!lifecycle.LoadingDataService())
            throw new InvalidOperationException("Failed to load data service; see previous log entries for the failing loader.");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
