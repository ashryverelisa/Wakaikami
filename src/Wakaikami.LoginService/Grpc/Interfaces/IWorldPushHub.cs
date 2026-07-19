using Wakaikami.Networking.Grpc.Messages.WorldPush;

namespace Wakaikami.LoginService.Grpc.Interfaces;

public interface IWorldPushHub
{
    public IAsyncEnumerable<WorldPush> Subscribe(int worldId, CancellationToken cancellationToken);
    public bool Broadcast(WorldPush push);
}
