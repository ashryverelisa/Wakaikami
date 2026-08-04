using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wakaikami.Content.World.Enums;
using Wakaikami.LoginService.Content.World;
using Wakaikami.LoginService.Tests.TestSupport;

namespace Wakaikami.LoginService.Tests.Content.World;

public sealed class WorldServerTests
{
    private readonly ILogger<WorldServer> _logger = NullLogger<WorldServer>.Instance;

    private WorldServer LiveServer(bool isTestServer = false) => new(_logger) { Info = Fakers.World(isTestServer: isTestServer) };

    [Fact]
    public void StatusWithoutRegistrationIsOfflineEvenAfterUpdateStatus()
    {
        var server = LiveServer();
        server.UpdateStatus(GameServerState.Low);

        Assert.Equal(GameServerState.Offline, server.Status);
    }

    [Fact]
    public void RegisterViaGrpcSetsIsReadyAndStatusLow()
    {
        var server = LiveServer();

        server.RegisterViaGrpc();

        Assert.True(server.IsReady);
        Assert.Equal(GameServerState.Low, server.Status);
    }

    [Fact]
    public void RegisterViaGrpcTestServerStatusIsReserved()
    {
        var server = LiveServer(isTestServer: true);

        server.RegisterViaGrpc();

        Assert.Equal(GameServerState.Reserved, server.Status);
    }

    [Fact]
    public void DisconnectGrpcClearsIsReadyAndReportsOffline()
    {
        var server = LiveServer();
        server.RegisterViaGrpc();

        server.DisconnectGrpc();

        Assert.False(server.IsReady);
        Assert.Equal(GameServerState.Offline, server.Status);
    }

    [Theory]
    [InlineData(GameServerState.Low)]
    [InlineData(GameServerState.Medium)]
    [InlineData(GameServerState.High)]
    [InlineData(GameServerState.Full)]
    public void UpdateStatusNonTestServerReportsLoadStatesVerbatim(GameServerState state)
    {
        var server = LiveServer(isTestServer: false);
        server.RegisterViaGrpc();

        server.UpdateStatus(state);

        Assert.Equal(state, server.Status);
    }

    [Theory]
    [InlineData(GameServerState.Low)]
    [InlineData(GameServerState.Medium)]
    [InlineData(GameServerState.High)]
    [InlineData(GameServerState.Full)]
    public void UpdateStatusTestServerWhenRegisteredMapsLoadStatesToReserved(GameServerState state)
    {
        var server = LiveServer(isTestServer: true);
        server.RegisterViaGrpc();

        server.UpdateStatus(state);

        Assert.Equal(GameServerState.Reserved, server.Status);
    }

    [Fact]
    public void UpdateStatusTestServerWhenRegisteredMaintenancePassesThrough()
    {
        var server = LiveServer(isTestServer: true);
        server.RegisterViaGrpc();

        server.UpdateStatus(GameServerState.Maintenance);

        Assert.Equal(GameServerState.Maintenance, server.Status);
    }
}
