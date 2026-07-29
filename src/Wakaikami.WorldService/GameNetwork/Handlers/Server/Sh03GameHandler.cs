using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Protocol.User;
using Wakaikami.WorldService.GameNetwork.Protocol.User.Server;

namespace Wakaikami.WorldService.GameNetwork.Handlers.Server;

public partial class Sh03GameHandler
{
    public void NcUserWillWorldSelectAck(WorldSession session, Guid id, bool added) => session.SendPacket(new NcUserWillWorldSelectAck(id, added));

    public void NcUserLoginWorldAck(WorldSession session) => session.SendPacket(new NcUserLoginWorldAck(session));

    public void NcUserLoginWorldFailAck(WorldSession session, UserErrorCodes errorCode) => session.SendPacket(new NcUserLoginWorldFailAck(errorCode));
}
