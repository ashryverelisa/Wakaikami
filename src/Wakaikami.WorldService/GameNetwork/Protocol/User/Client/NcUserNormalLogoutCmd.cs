using Wakaikami.Networking.Protocol.Fiesta;
using Wakaikami.Networking.Protocol.Fiesta.Enums;

namespace Wakaikami.WorldService.GameNetwork.Protocol.User.Client;

[GenerateSerialization]
public partial class NcUserNormalLogoutCmd(byte[] buffer) : FiestaClientPacket(buffer)
{
    [Field(0, As = typeof(byte))]
    public LogoutType LogoutType { get; private set; }
}
