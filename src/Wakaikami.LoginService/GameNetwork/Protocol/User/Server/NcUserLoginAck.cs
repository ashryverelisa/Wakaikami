using Wakaikami.Content.World.Enums;
using Wakaikami.LoginService.Content.World;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.LoginService.GameNetwork.Protocol.User.Server;

public class NcUserLoginAck(FiestaHandlerType header, ushort type, IReadOnlyList<WorldServer>? serverList = null) : FiestaServerPacket(header, type)
{
    private readonly IReadOnlyList<WorldServer>? _serverList = serverList;

    public NcUserLoginAck()
        : this(FiestaHandlerType.User, GameHandler03Type.NcUserLoginAck, null) { }

    public NcUserLoginAck(IReadOnlyList<WorldServer> serverList)
        : this() => _serverList = serverList;

    public override void Write()
    {
        var worlds = _serverList ?? [];

        Writer.Write((byte)worlds.Count);

        foreach (var world in worlds)
        {
            Writer.Write(world.Info.Id);
            Writer.WriteString(world.Info.WorldName, 16);
            Writer.Write((byte)(world.Status is GameServerState.Reserved or GameServerState.Maintenance ? GameServerState.Maintenance : world.Status));
        }
    }
}
