using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.Networking.Grpc.Extensions;
using Wakaikami.Networking.Grpc.Messages.ZoneWorldAccount;
using Wakaikami.ZoneService.Grpc.Handlers.Servers;

namespace Wakaikami.ZoneService.Grpc;

public sealed class GrpcZoneSh03ClusterHandler(ZoneWorldAccount.ZoneWorldAccountClient client, ILogger<GrpcZoneSh03ClusterHandler> logger) : ISh03ClusterHandler
{
    public async Task<GameAccount?> NcAccountSessionGet(ushort sessionId)
    {
        var reply = await client.GetAccountSessionAsync(new AccountSessionRequest { SessionId = sessionId });
        return reply.Found
            ? new GameAccount
            {
                Id = reply.AccountId,
                Name = reply.AccountName,
                SessionId = sessionId,
            }
            : null;
    }

    public void NcAccountSessionCut(ushort sessionId) => client.SessionCutAsync(new SessionCutRequest { SessionId = sessionId }).Forget(logger);
}
