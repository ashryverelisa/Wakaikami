using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.Content.Protocol.Misc.Server;
using Wakaikami.LoginService.GameNetwork.Protocol.User.Server;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.WorldService.GameNetwork.Protocol.Misc.Server;

namespace Wakaikami.WorldService.GameNetwork.Listening;

public class WorldSession(Socket pSocket, FiestaHandlerStore fiestaStore, ILogger logger) : AccountSession(pSocket, fiestaStore, logger)
{
    public override void SendHeartBeat() => SendPacket(new NcMiscHeartbeatAck());

    public override void SendHandShake() => SendPacket(new NcMiscSeedAck(Crypto.XorPosition));

    public override void Disconnect(bool notifyPeer)
    {
        if (notifyPeer)
        {
            SendPacket(new NcUserConnectionCutCmd());
        }
        Dispose();
    }
}
