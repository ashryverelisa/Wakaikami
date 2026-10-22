using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Protocol.Avatar;
using Wakaikami.WorldService.GameNetwork.Protocol.Avatar.Server;

namespace Wakaikami.WorldService.GameNetwork.Handlers.Server;

public partial class Sh05GameHandler
{
    public void NcAvatarCreateFailAck(WorldSession session, AvatarErrors error) => session.SendPacket(new NcAvatarCreateFailAck(error));

    public void NcAvatarCreateSuccessAck(WorldSession session, CharacterListEntry character) => session.SendPacket(new NcAvatarCreateSuccessAck(character));

    public void NcAvatarEraseSuccessAck(WorldSession session, byte slot) => session.SendPacket(new NcAvatarEraseSuccessAck(slot));

    public void NcAvatarEraseFailAck(WorldSession session, AvatarErrors error) => session.SendPacket(new NcAvatarEraseFailAck(error));
}
