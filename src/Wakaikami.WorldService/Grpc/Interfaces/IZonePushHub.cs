using Wakaikami.Networking.Grpc.Messages.ZonePush;

namespace Wakaikami.WorldService.Grpc.Interfaces;

public interface IZonePushHub
{
    public IAsyncEnumerable<ZonePush> Subscribe(int zoneId, CancellationToken cancellationToken);
    public bool Publish(int zoneId, ZonePush push);
    public bool Broadcast(ZonePush push);
}
