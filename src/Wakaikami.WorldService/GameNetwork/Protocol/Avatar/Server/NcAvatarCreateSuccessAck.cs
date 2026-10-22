using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;
using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Proto;

namespace Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Server;

public class NcAvatarCreateSuccessAck(CharacterListEntry character) : FiestaServerPacket(FiestaHandlerType.Avatar, GameHandler05Type.NcAvatarCreateSuccessAck)
{
    private CharacterListEntry _character = character;

    public override void Write()
    {
        Writer.Write(_character.Slot);
        AvatarInformation.Write(Writer, _character);
    }

    protected override void DisposeInternal()
    {
        _character = null!;
        base.DisposeInternal();
    }
}
