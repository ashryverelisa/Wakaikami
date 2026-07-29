using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Content.Account;
using Wakaikami.Networking.Grpc.Extensions;
using Wakaikami.Networking.Grpc.Messages.WorldLoginAccount;
using Wakaikami.WorldService.Configuration;
using Wakaikami.WorldService.Grpc.Handlers.Server;

namespace Wakaikami.WorldService.Grpc;

public sealed class GrpcWorldSh03ClusterHandler(
    WorldLoginAccount.WorldLoginAccountClient client,
    IOptions<WorldOptions> options,
    ILogger<GrpcWorldSh03ClusterHandler> logger
) : ISh03ClusterHandler
{
    // Login tracks which world holds an account, so every account report has to say who is reporting.
    private int WorldId => options.Value.WorldInfo.WorldId;

    public void NcAccountOffline(GameAccount account) =>
        client.AccountOfflineAsync(new AccountOfflineRequest { AccountId = account.Id, WorldId = WorldId }).Forget(logger);

    public void NcAccountOnlineList(int[] accountIds) =>
        client.AccountOnlineListAsync(new AccountOnlineListRequest { AccountIds = { accountIds }, WorldId = WorldId }).Forget(logger);

    public async Task<TransferGenResult> NcAccountTransferGenReq(GameAccount account)
    {
        var reply = await client.TransferGenAsync(new TransferGenRequest { AccountId = account.Id, AccountName = account.Name });
        return new TransferGenResult(Guid.TryParse(reply.AuthId, out var authId) ? authId : Guid.Empty, reply.Success);
    }

    public async Task<GameAccount?> NcAccountTransferGetReq(Guid transferId, string accountName)
    {
        // Fetching the transfer hands the account over to this world, hence the world id.
        var reply = await client.TransferGetAsync(
            new TransferGetRequest
            {
                TransferId = transferId.ToString(),
                AccountName = accountName,
                WorldId = WorldId,
            }
        );
        return reply.Found ? new GameAccount { Id = reply.AccountId, Name = reply.AccountName } : null;
    }
}
