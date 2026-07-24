using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

[GenerateSerialization]
[Const(0, (byte)181)] // TODO: unk need to check -> Some ErrorCode for the Client? or empty?
public partial class NcUserClientWrongVersionCheckAck() : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserClientWrongVersionCheckAck);
