using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.ZoneService.GameNetwork.Protocol.Misc.Server;

public sealed class NcMiscSeedAck(ushort seed) : FiestaServerPacket(FiestaHandlerType.Misc, GameHandler02Type.NcMiscSeedAck)
{
    public override void Write()
    {
        Writer.Write(seed);
    }
}
