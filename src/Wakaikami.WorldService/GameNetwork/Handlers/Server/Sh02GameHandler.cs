using Wakaikami.WorldService.Content.Entity.Character;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Protocol.Misc.Server;

namespace Wakaikami.WorldService.GameNetwork.Handlers.Server;

public partial class Sh02GameHandler
{
    public void NcGameTimeAck(WorldSession session) => session.SendPacket(new NcGameTimeAck());

    public void NcServerTimeNotifyCmd(WorldCharacter character) => character.Send(new NcServerTimeNotifyCmd());
}
