using Wakaikami.Content.Account;

namespace Wakaikami.WorldService.Grpc.Handlers.Server;

public interface ISh03ClusterHandler
{
    public void NcAccountOffline(GameAccount account);
    public void NcAccountOnlineList(int[] accountIds);
    public Task<TransferGenResult> NcAccountTransferGenReq(GameAccount account);
    public Task<GameAccount?> NcAccountTransferGetReq(Guid transferId, string accountName);
}
