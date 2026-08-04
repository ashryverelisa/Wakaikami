using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wakaikami.Content.World.Enums;
using Wakaikami.LoginService.Content.World;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.LoginService.Grpc;
using Wakaikami.LoginService.Tests.TestSupport;
using Wakaikami.Networking.Grpc.Messages.WorldLoginRegistration;

namespace Wakaikami.LoginService.Tests.Grpc;

public sealed class WorldLoginRegistrationServiceTests
{
    private readonly IWorldServerManager _worldServers = Substitute.For<IWorldServerManager>();
    private readonly ILogger<WorldLoginRegistrationService> _logger = NullLogger<WorldLoginRegistrationService>.Instance;

    private WorldLoginRegistrationService CreateService() => new(_worldServers, _logger);

    private static RegisterWorldRequest ValidRequest(int worldId = 1) =>
        new()
        {
            WorldId = worldId,
            WorldName = "Enid",
            ConnectIp = "127.0.0.1",
            ConnectPort = 9120,
            IsTestServer = true,
        };

    private void GivenKnownWorld(sbyte worldId, WorldServer server) =>
        _worldServers
            .GetWorldById(worldId, out Arg.Any<WorldServer?>())
            .Returns(call =>
            {
                call[1] = server;
                return true;
            });

    [Fact]
    public async Task RegisterWorldValidRequestRegistersAnnouncedInfoAndAccepts()
    {
        WorldInfo? registered = null;
        var server = new WorldServer(NullLogger<WorldServer>.Instance);
        _worldServers
            .Register(Arg.Any<WorldInfo>())
            .Returns(call =>
            {
                registered = call.Arg<WorldInfo>();
                server.Info = registered;
                return server;
            });
        _worldServers.GetWorldById(1, out Arg.Any<WorldServer?>()).Returns(false);

        var result = await CreateService().RegisterWorld(ValidRequest(), GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.True(result.Accepted);
        Assert.True(server.IsReady);
        Assert.NotNull(registered);
        Assert.Equal(1, registered.Id);
        Assert.Equal("Enid", registered.WorldName);
        Assert.Equal("127.0.0.1", registered.Ip);
        Assert.Equal(9120, registered.Port);
        Assert.True(registered.IsTestServer);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(200)] // outside sbyte range
    public async Task RegisterWorldInvalidWorldIdReturnsRejected(int worldId)
    {
        var result = await CreateService().RegisterWorld(ValidRequest(worldId), GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.False(result.Accepted);
        _worldServers.DidNotReceive().Register(Arg.Any<WorldInfo>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a-name-that-is-far-too-long")] // the client packet field is 16 bytes
    public async Task RegisterWorldInvalidNameReturnsRejected(string worldName)
    {
        var request = ValidRequest();
        request.WorldName = worldName;

        var result = await CreateService().RegisterWorld(request, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.False(result.Accepted);
        _worldServers.DidNotReceive().Register(Arg.Any<WorldInfo>());
    }

    [Fact]
    public async Task RegisterWorldEmptyConnectIpReturnsRejected()
    {
        var request = ValidRequest();
        request.ConnectIp = "";

        var result = await CreateService().RegisterWorld(request, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.False(result.Accepted);
        _worldServers.DidNotReceive().Register(Arg.Any<WorldInfo>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public async Task RegisterWorldInvalidPortReturnsRejected(int port)
    {
        var request = ValidRequest();
        request.ConnectPort = port;

        var result = await CreateService().RegisterWorld(request, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.False(result.Accepted);
        _worldServers.DidNotReceive().Register(Arg.Any<WorldInfo>());
    }

    [Fact]
    public async Task RegisterWorldIdStillConnectedReturnsRejected()
    {
        var connected = new WorldServer(NullLogger<WorldServer>.Instance) { Info = Fakers.World(id: 1) };
        connected.RegisterViaGrpc();
        GivenKnownWorld(1, connected);

        var result = await CreateService().RegisterWorld(ValidRequest(), GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.False(result.Accepted);
        _worldServers.DidNotReceive().Register(Arg.Any<WorldInfo>());
    }

    [Fact]
    public async Task RegisterWorldKnownButDisconnectedWorldReregisters()
    {
        var server = new WorldServer(NullLogger<WorldServer>.Instance) { Info = Fakers.World(id: 1) };
        server.RegisterViaGrpc();
        server.DisconnectGrpc(); // e.g. the world's push stream dropped
        GivenKnownWorld(1, server);
        _worldServers
            .Register(Arg.Any<WorldInfo>())
            .Returns(call =>
            {
                server.Info = call.Arg<WorldInfo>();
                return server;
            });

        var result = await CreateService().RegisterWorld(ValidRequest(), GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.True(result.Accepted);
        Assert.True(server.IsReady);
    }

    [Fact]
    public async Task WorldSyncExistingWorldUpdatesStatus()
    {
        var server = new WorldServer(NullLogger<WorldServer>.Instance) { Info = Fakers.World(id: 1) };
        server.RegisterViaGrpc();
        GivenKnownWorld(1, server);

        await CreateService()
            .WorldSync(new WorldSyncRequest { WorldId = 1, State = (int)GameServerState.Medium }, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.Equal(GameServerState.Medium, server.Status);
    }

    [Fact]
    public async Task WorldSyncUnknownWorldIdNoOp()
    {
        _worldServers.GetWorldById(99, out Arg.Any<WorldServer?>()).Returns(false);

        // Should not throw.
        await CreateService()
            .WorldSync(new WorldSyncRequest { WorldId = 99, State = (int)GameServerState.Full }, GrpcTest.Context(TestContext.Current.CancellationToken));
    }
}
