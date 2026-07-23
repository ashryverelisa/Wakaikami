using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Client;

// TODO: right struct -> check Source Gen
// unsigned __int8 XTrapClientKeyLength;
// unsigned __int8 XTrapClientKey[];
[GenerateSerialization]
public sealed partial class NcUserXTrapReq(byte[] buffer) : FiestaClientPacket(buffer)
{
    [Field(0, Length = 30)]
    public string XTrapKey { get; private set; } = null!;
}
