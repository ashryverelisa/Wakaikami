using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.Content.Protocol.Misc.Server;

public class NcMiscSeedAck(ushort seed) : FiestaServerPacket(FiestaHandlerType.Misc, GameHandler02Type.NcMiscSeedAck)
{
    public override void Write()
    {
        Writer.Write(seed);
    }
}
