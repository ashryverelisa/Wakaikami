using Wakaikami.Content.World;
using Wakaikami.Content.World.Enums;

namespace Wakaikami.WorldService.Content.Zone;

public class ZoneServer(byte zoneId)
{
    private readonly Lock _threadLocker = new();

    public byte ZoneId { get; } = zoneId;
    public ZoneConnectInfo ConnectInfo { get; private set; } = null!;
    public bool IsReady { get; set; }

    public bool IsConnected => IsReady;

    public GameServerState Status
    {
        get
        {
            if (!IsConnected)
                return GameServerState.Offline;

            return ConnectInfo.IsTestServer ? GameServerState.Reserved : GameServerStatus.Calculate(ConnectInfo.NowPlayer, ConnectInfo.MaxPlayer);
        }
    }

    public void Dispose()
    {
        lock (_threadLocker)
        {
            ConnectInfo = null!;
            IsReady = false;
        }
    }

    public void RegisterViaGrpc(ZoneConnectInfo connectInfo)
    {
        lock (_threadLocker)
        {
            ConnectInfo = connectInfo;
            IsReady = true;
        }
    }

    public void DisconnectGrpc()
    {
        lock (_threadLocker)
        {
            ConnectInfo = null!;
            IsReady = false;
        }
    }

    public void Update(int nowPlayer) => ConnectInfo.NowPlayer = nowPlayer;
}
