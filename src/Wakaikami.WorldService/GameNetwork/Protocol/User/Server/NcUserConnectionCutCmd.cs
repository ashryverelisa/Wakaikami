using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.User.Server;

//Todo Reverse more...
[GenerateSerialization]
[Const(0, (ushort)1667)]
public partial class NcUserConnectionCutCmd() : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserConnectCutCmd);
