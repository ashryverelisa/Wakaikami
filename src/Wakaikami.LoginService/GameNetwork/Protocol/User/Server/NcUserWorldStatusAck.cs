using Wakaikami.LoginService.Content.World;
using Wakaikami.Networking.HandlerTypes;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

public sealed class NcUserWorldStatusAck(IReadOnlyList<WorldServer> serverList)
    : NcUserLoginAck(FiestaHandlerType.User, GameHandler03Type.NcUserWorldStatusAck, serverList);
