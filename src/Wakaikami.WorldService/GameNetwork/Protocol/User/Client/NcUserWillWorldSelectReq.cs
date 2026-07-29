using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.User.Client;

public class NcUserWillWorldSelectReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    public override bool Read() => true;
}
