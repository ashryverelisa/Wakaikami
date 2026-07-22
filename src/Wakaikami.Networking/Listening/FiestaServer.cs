using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.Session;

namespace Wakaikami.Networking.Listening;

public abstract class FiestaServer<TSession>(ILoggerFactory loggerFactory) : ServerBase<TSession>(loggerFactory)
    where TSession : FiestaSession
{
    protected abstract override TSession CreateSession(Socket socket);

    protected override bool AcceptSession(TSession session) => true;

    protected override void OnSessionStarted(TSession session) => session.SendHandShake();
}
