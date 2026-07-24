using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.ZoneService.GameNetwork.Protocol.Misc.Server;

public class NcHeartbeatAck() : FiestaServerPacket(FiestaHandlerType.Misc, GameHandler02Type.NcMiscHeartbeatReq)
{
    public override void Write() { }
}
