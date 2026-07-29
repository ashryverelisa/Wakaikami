using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.User.Server;

[GenerateSerialization]
public partial class NcUserLoginWorldFailAck([Field(0, As = typeof(ushort))] UserErrorCodes errorCode)
    : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserLoginWorldFailAck);
