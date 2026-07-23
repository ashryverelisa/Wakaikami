using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

[GenerateSerialization]
public sealed partial class NcUserLoginFailAck([Field(0, As = typeof(ushort))] UserErrors Code)
    : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserLoginFailAck);
