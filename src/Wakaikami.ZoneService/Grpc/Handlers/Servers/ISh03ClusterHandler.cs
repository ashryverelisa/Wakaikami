using Wakaikami.Content.Account;

namespace Wakaikami.ZoneService.Grpc.Handlers.Servers;

public interface ISh03ClusterHandler
{
    public Task<GameAccount?> NcAccountSessionGet(ushort sessionId);
    public void NcAccountSessionCut(ushort sessionId);
}
