using Wakaikami.Content.Account;
using Wakaikami.Content.World.Enums;
using Wakaikami.LoginService.Content.World;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.LoginService.GameNetwork.Listening;
using Wakaikami.LoginService.GameNetwork.Protocol.User;
using Wakaikami.LoginService.GameNetwork.Protocol.User.Server;
using Wakaikami.Networking.Session;

namespace Wakaikami.LoginService.GameNetwork.Handlers.Server;

public partial class Sh03GameHandler(IWorldServerManager worldServers)
{
    public void NcUserConnectionCutCmd(FiestaSession session) => session.SendPacket(new NcUserConnectionCutCmd());

    public void NcUserLoginFailAck(AccountSession session, UserErrors code) => session.SendPacket(new NcUserLoginFailAck(code));

    public void NcUserLoginAck(LoginSession session) => session.SendPacket(new NcUserLoginAck(worldServers.ToList()));

    public void NcUserClientWrongVersionCheckAck(LoginSession session) => session.SendPacket(new NcUserClientWrongVersionCheckAck());

    public void NcUserClientRightVersionCheckAck(LoginSession session) => session.SendPacket(new NcUserClientRightVersionCheckAck());

    public void NcUserXTrapAck(LoginSession session) => session.SendPacket(new NcUserXTrapAck());

    public void NcUserWorldStatusAck(LoginSession session) => session.SendPacket(new NcUserWorldStatusAck(worldServers.ToList()));

    public void NcUserWorldSelectAck(LoginSession session, Guid authId, WorldServer server, GameServerState newState) =>
        session.SendPacket(new NcUserWorldSelectAck(authId, server, newState));
}
