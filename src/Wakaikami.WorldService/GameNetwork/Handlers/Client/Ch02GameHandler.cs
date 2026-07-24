using Wakaikami.Core.Time;
using Wakaikami.Networking.HandlerStores.Attributes;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Protocol.Misc.Client;

namespace Wakaikami.WorldService.GameNetwork.Handlers.Client;

[GameHandlerClass(FiestaHandlerType.Misc)]
public class Ch02GameHandler
{
    [GameHandler(GameHandler02Type.NcMiscHeartbeatReq, typeof(NcMiscHeartbeatReq))]
    public void NcMiscHeartbeatReq(WorldSession session, NcMiscHeartbeatReq packet)
    {
        session.ConnectionInfo.LastSync = ServerClock.UtcNow;
    }
}
