using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Misc.Server;

public class NcMiscHeartbeatAck() : FiestaServerPacket(FiestaHandlerType.Misc, GameHandler02Type.NcMiscHeartbeatAck)
{
    public override void Write() { }
}
