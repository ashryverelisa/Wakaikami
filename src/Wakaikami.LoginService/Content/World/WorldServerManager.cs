using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Wakaikami.LoginService.Content.World.Interfaces;

namespace Wakaikami.LoginService.Content.World;

public sealed class WorldServerManager(ILogger<WorldServer> worldLogger) : IWorldServerManager
{
    private readonly ConcurrentDictionary<sbyte, WorldServer> _worldServers = new();

    public IReadOnlyList<WorldServer> ToList() => [.. _worldServers.Values.OrderBy(s => s.Info.Id)];

    public bool GetWorldById(sbyte worldId, [NotNullWhen(true)] out WorldServer? server) => _worldServers.TryGetValue(worldId, out server);

    public WorldServer Register(WorldInfo info) =>
        _worldServers.AddOrUpdate(
            info.Id,
            _ => new WorldServer(worldLogger) { Info = info },
            (_, existing) =>
            {
                // Re-registration (reconnect or changed world config): refresh the announced parameters.
                existing.Info = info;
                return existing;
            }
        );
}
