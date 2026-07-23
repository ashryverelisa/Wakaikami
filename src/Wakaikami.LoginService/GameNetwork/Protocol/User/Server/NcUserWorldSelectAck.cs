using Wakaikami.Content.World.Enums;
using Wakaikami.LoginService.Content.World;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

public sealed class NcUserWorldSelectAck(Guid authId, WorldServer server, GameServerState newState)
    : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserWorldSelectAck)
{
    public override void Write()
    {
        Writer.Write((byte)newState);
        Writer.WriteString(server.Info.Ip, 16);
        Writer.Write(server.Info.Port);
        Writer.WriteGuid(authId); //32 Bytes after that also just a transfer token
    }
}
