using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.WorldService.Configuration;

namespace Wakaikami.WorldService.Content.Zone;

public class ZoneManager(IOptions<WorldOptions> options, ILogger<ZoneManager> logger) : IGameServerModule
{
    private ConcurrentDictionary<byte, ZoneServer> _zonesById = new();

    InitialType IModule<GameInitialStage>.InitialType => InitialType.World;
    GameInitialStage IModule<GameInitialStage>.Stage => GameInitialStage.Zone;

    Task<bool> IGameServerModule.InitializeAsync(CancellationToken cancellationToken)
    {
        var maxConnection = options.Value.ZoneInfo.ServerInfo.MaxConnection;

        if (maxConnection < 1)
        {
            logger.InvalidAwaitingZoneCount();
            return Task.FromResult(false);
        }

        _zonesById = new ConcurrentDictionary<byte, ZoneServer>();

        for (byte i = 0; i < maxConnection; i++)
        {
            _zonesById.TryAdd(i, new ZoneServer(i));
        }

        return Task.FromResult(true);
    }

    public bool GetZoneById(byte zoneId, [NotNullWhen(true)] out ZoneServer? zone, bool onlyConnect = true)
    {
        return _zonesById.TryGetValue(zoneId, out zone) && (!onlyConnect || zone.IsConnected);
    }
}
