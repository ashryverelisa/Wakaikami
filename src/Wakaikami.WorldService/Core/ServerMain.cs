using Microsoft.Extensions.Options;
using Wakaikami.Content.World;
using Wakaikami.Content.World.Enums;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.WorldService.Configuration;

namespace Wakaikami.WorldService.Core;

public class ServerMain(IServiceProvider services, IOptions<WorldOptions> worldOptions) : ServerMainBase(InitialType.World, services)
{
    public bool IsTestServer { get; set; }

    public GameServerState Status
    {
        get
        {
            if (!LoadedGameServer)
                return GameServerState.Maintenance;

            return IsTestServer ? GameServerState.Reserved : GameServerStatus.Calculate(0, worldOptions.Value.WorldInfo.MaxPlayer);
        }
    }
}
