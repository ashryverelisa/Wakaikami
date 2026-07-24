using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Client;

[GenerateSerialization]
public sealed partial class NcUserLoginWithOptReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    [Field(0)]
    public Guid Id { get; private set; }
}