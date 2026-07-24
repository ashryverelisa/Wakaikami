using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

// TODO: right struct -> is the Flag default true or some result from xtrap?
[GenerateSerialization]
[Const(0, true)]
public sealed partial class NcUserXTrapAck() : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserXTrapAck);
