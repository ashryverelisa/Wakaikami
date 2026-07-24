using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Client;

[GenerateSerialization]
public partial class NcUserUsLoginReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    [Field(0, Length = 260)]
    public string Username { get; private set; } = null!;

    [Field(1, Length = 36)]
    public string PasswordHash { get; private set; } = null!;

    [Field(2, Length = 20)]
    public string Original { get; private set; } = null!;
}
