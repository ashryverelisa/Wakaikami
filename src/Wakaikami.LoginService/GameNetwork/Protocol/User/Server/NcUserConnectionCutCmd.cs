using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

// TODO: right struct
[GenerateSerialization]
[Const(0, (ushort)UserErrors.ConnectionCut)]
public partial class NcUserConnectionCutCmd() : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserConnectcutCmd);
