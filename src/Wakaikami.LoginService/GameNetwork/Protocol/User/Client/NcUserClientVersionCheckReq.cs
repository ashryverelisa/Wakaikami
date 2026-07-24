using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Client;

[GenerateSerialization]
public sealed partial class NcUserClientVersionCheckReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    [Field(0, Length = 64)]
    public string VersionKey { get; private set; } = null!;
}
