namespace Wakaikami.Networking.Grpc.Health;

/// <summary>Connection state of an inter-service push stream subscription.</summary>
public interface IPushStreamStatus
{
    public bool IsStreamConnected { get; }
}
