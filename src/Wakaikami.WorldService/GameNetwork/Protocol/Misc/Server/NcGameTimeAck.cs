using Wakaikami.Core.Time;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Misc.Server;

public class NcGameTimeAck() : FiestaServerPacket(FiestaHandlerType.Misc, GameHandler02Type.NcGameTimeAck)
{
    public override void Write()
    {
        var now = ServerClock.LocalNow;
        Writer.Write((byte)now.Hour);
        Writer.Write((byte)now.Minute);
        Writer.Write((byte)now.Second);
    }
}
