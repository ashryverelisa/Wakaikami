using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Client;

// Body-less request; the serialization generator skips field-less packets, so Read() stays manual.
public class NcUserWorldStatusReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    public override bool Read() => true;
}
