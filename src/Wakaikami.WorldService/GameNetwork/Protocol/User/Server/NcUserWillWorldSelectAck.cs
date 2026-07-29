using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta;

namespace Wakaikami.WorldService.GameNetwork.Protocol.User.Server;

public class NcUserWillWorldSelectAck(Guid id, bool added) : FiestaServerPacket(FiestaHandlerType.User, GameHandler03Type.NcUserWillWorldSelectAck)
{
    public override void Write()
    {
        // TODO: check codes
        Writer.Write(added ? (ushort)7768 : (ushort)73);
        Writer.WriteGuid(id);
    }
}
