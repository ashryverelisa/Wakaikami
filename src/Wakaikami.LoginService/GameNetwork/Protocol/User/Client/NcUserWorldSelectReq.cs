using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Client;

[GenerateSerialization]
public partial class NcUserWorldSelectReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    [Field(0)]
    public sbyte WorldId { get; private set; }
}
