using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Client;

[GenerateSerialization]
public partial class NcUserUsLoginReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    //EMail or Accname.. region spezefics?
    [Field(0, Length = 256)]
    public string Username { get; private set; } = null!;

    [Field(1)]
    public int Unk1 { get; private set; }

    [Field(2, Length = 32)]
    public string PasswordHash { get; private set; } = null!;

    [Field(3)]
    public int Unk2 { get; private set; }

    [Field(4, Length = 20)]
    public string Original { get; private set; } = null!;
}
