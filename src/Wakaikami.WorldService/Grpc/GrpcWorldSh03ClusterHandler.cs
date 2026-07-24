using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Content.Account;
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
    private int WorldId => options.Value.WorldInfo.WorldId;

    public void NcAccountOffline(GameAccount account)
    {
        throw new NotImplementedException();
    }

    public void NcAccountOnlineList(int[] accountIds)
    {
        throw new NotImplementedException();
    }

    public Task<TransferGenResult> NcAccountTransferGenReq(GameAccount account)
    {
        throw new NotImplementedException();
    }

    public Task<GameAccount?> NcAccountTransferGetReq(Guid transferId, string accountName)
    {
        throw new NotImplementedException();
    }
}
