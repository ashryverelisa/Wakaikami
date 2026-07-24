using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Wakaikami.Networking.Grpc.Messages.ZoneWorldAccount;
using Wakaikami.WorldService.GameNetwork.Listening.Interfaces;

namespace Wakaikami.WorldService.Grpc;

public sealed class ZoneWorldAccountService(IWorldSessionManager worldSessions) : ZoneWorldAccount.ZoneWorldAccountBase
{
    public override Task<AccountSessionReply> GetAccountSession(AccountSessionRequest request, ServerCallContext context)
    {
        if (worldSessions.GetSessionBySessionId((ushort)request.SessionId, out var session) && session?.Account is not null)
        {
            return Task.FromResult(
                new AccountSessionReply
                {
                    Found = true,
                    AccountId = session.Account.Id,
                    AccountName = session.Account.Name,
                }
            );
        }

        return Task.FromResult(new AccountSessionReply { Found = false });
    }

    public override Task<Empty> SessionCut(SessionCutRequest request, ServerCallContext context)
    {
        if (!worldSessions.GetSessionBySessionId((ushort)request.SessionId, out var session) || session is null)
            return Task.FromResult(new Empty());

        session.Dispose();
        return Task.FromResult(new Empty());
    }
}
