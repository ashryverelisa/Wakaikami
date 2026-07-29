using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Misc.Client;

public class NcGameTimeReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    public override bool Read() => true;
}
