using Microsoft.Extensions.Hosting;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.Updates;

namespace Wakaikami.Core.Hosting;

public sealed class ServerMainBootstrapHostedService(
    IServerLifecycle lifecycle,
    UpdateManager updateManager
) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = lifecycle;
        updateManager.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
