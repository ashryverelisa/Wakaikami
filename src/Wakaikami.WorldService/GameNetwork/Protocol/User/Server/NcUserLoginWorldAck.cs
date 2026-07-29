using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Proto;

namespace Wakaikami.WorldService.GameNetwork.Protocol.User.Server;

public class NcUserLoginWorldAck(WorldSession session) : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserLoginWorldAck)
{
    private WorldSession _session = session;

    public override void Write()
    {
        var characters = _session.CharacterList.ToArray();

        Writer.Write(_session.SessionId);
        Writer.Write((byte)characters.Length);

        foreach (var character in characters)
            AvatarInformation.Write(Writer, character);
    }

    protected override void DisposeInternal()
    {
        base.DisposeInternal();
        _session = null!;
    }
}
