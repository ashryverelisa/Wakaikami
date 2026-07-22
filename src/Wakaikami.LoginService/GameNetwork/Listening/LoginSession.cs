using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.Content.Protocol.Misc.Server;
using Wakaikami.LoginService.GameNetwork.Protocol.User.Server;
using Wakaikami.Networking.HandlerStores;

namespace Wakaikami.LoginService.GameNetwork.Listening;

public sealed class LoginSession(Socket pSocket, FiestaHandlerStore fiestaStore, ILogger logger) : AccountSession(pSocket, fiestaStore, logger)
{
    public override void SendHandShake() => SendPacket(new NcMiscSeedAck(Crypto.XorPosition));

    public override void Disconnect(bool notifyPeer)
    {
        if (notifyPeer)
            SendPacket(new NcUserConnectionCutCmd());

        base.Dispose();
    }
}
