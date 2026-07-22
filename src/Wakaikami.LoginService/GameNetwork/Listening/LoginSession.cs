using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.Networking.HandlerStores;

namespace Wakaikami.LoginService.GameNetwork.Listening;

public sealed class LoginSession(Socket pSocket, FiestaHandlerStore fiestaStore, ILogger logger) : AccountSession(pSocket, fiestaStore, logger)
{
    public override void SendHandShake()
    {
        throw new NotImplementedException();
    }

    public override void Disconnect(bool notifyPeer)
    {
        throw new NotImplementedException();
    }
}
