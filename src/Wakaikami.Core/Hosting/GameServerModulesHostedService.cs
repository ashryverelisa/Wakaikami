using Microsoft.Extensions.Hosting;
using Wakaikami.Core.Hosting.Interfaces;

namespace Wakaikami.Core.Hosting;

public sealed class GameServerModulesHostedService(IServerLifecycle lifecycle) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!await lifecycle.LoadGameServerModulesAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Failed to load game server modules; see previous log entries for the failing module."
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
