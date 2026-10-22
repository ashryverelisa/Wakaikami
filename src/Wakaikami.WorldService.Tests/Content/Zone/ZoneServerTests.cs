using Wakaikami.Content.World.Enums;
using Wakaikami.WorldService.Content.Zone;

namespace Wakaikami.WorldService.Tests.Content.Zone;

public sealed class ZoneServerTests
{
    private static ZoneServer NewZone(byte zoneId = 1) => new(zoneId);

    private static ZoneConnectInfo ConnectInfo(int nowPlayer = 0, ushort maxPlayer = 100, bool isTestServer = false) =>
        new()
        {
            ConnectIp = "127.0.0.1",
            ConnectPort = 9230,
            NowPlayer = nowPlayer,
            MaxPlayer = maxPlayer,
            IsTestServer = isTestServer,
        };

    [Fact]
    public void BeforeRegistrationIsNotConnectedAndReportsOffline()
    {
        var zone = NewZone();

        Assert.False(zone.IsConnected);
        Assert.Equal(GameServerState.Offline, zone.Status);
    }

    [Fact]
    public void RegisterViaGrpcMakesTheSlotConnectedAndStoresTheAnnouncedInfo()
    {
        var zone = NewZone();
        var info = ConnectInfo();

        zone.RegisterViaGrpc(info);

        Assert.True(zone.IsConnected);
        Assert.Same(info, zone.ConnectInfo);
    }

    [Fact]
    public void StatusWhenRegisteredTracksTheReportedLoad()
    {
        var zone = NewZone();
        zone.RegisterViaGrpc(ConnectInfo(nowPlayer: 60, maxPlayer: 100));

        Assert.Equal(GameServerState.High, zone.Status);
    }

    [Fact]
    public void StatusTestZoneIsAlwaysReservedRegardlessOfLoad()
    {
        var zone = NewZone();
        zone.RegisterViaGrpc(ConnectInfo(nowPlayer: 0, maxPlayer: 100, isTestServer: true));

        Assert.Equal(GameServerState.Reserved, zone.Status);
    }

    [Fact]
    public void UpdateRecordsTheNewPlayerCountAndMovesTheState()
    {
        var zone = NewZone();
        zone.RegisterViaGrpc(ConnectInfo(nowPlayer: 0, maxPlayer: 100));

        zone.Update(60);

        Assert.Equal(60, zone.ConnectInfo.NowPlayer);
        Assert.Equal(GameServerState.High, zone.Status);
    }

    [Fact]
    public void DisconnectGrpcReleasesTheSlot()
    {
        var zone = NewZone();
        zone.RegisterViaGrpc(ConnectInfo());

        zone.DisconnectGrpc();

        Assert.False(zone.IsConnected);
        Assert.Equal(GameServerState.Offline, zone.Status);
    }

    [Fact]
    public void RegisterViaGrpcAfterDisconnectReusesTheSlot()
    {
        var zone = NewZone();
        zone.RegisterViaGrpc(ConnectInfo());
        zone.DisconnectGrpc();

        var reconnected = ConnectInfo(nowPlayer: 10, maxPlayer: 100);
        zone.RegisterViaGrpc(reconnected);

        Assert.True(zone.IsConnected);
        Assert.Same(reconnected, zone.ConnectInfo);
    }

    [Fact]
    public void DisposeReleasesTheSlot()
    {
        var zone = NewZone();
        zone.RegisterViaGrpc(ConnectInfo());

        zone.Dispose();

        Assert.False(zone.IsConnected);
        Assert.Equal(GameServerState.Offline, zone.Status);
    }
}
