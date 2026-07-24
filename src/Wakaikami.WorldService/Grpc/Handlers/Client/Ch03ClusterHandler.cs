using Wakaikami.WorldService.GameNetwork.Listening.Interfaces;

namespace Wakaikami.WorldService.Grpc.Handlers.Client;

public class Ch03ClusterHandler(IWorldSessionManager worldSessions)
{
    public void HandleAccountStateFailed(int accountId)
    {
        if (worldSessions.GetAccountById(accountId, out var gameSession) && gameSession is not null)
        {
            gameSession.Disconnect(notifyPeer: true);
        }
    }
}
