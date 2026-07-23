using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

[GenerateSerialization]
[Const(0, (byte)106)] // TODO: right struct -> check source gen
// unsigned __int8 XTrapServerKeyLength;
// unsigned __int8 XTrapServerKey[];
public partial class NcUserClientRightVersionCheckAck() : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserClientRightVersionCheckAck);
