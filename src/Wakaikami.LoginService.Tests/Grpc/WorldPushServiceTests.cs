using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.World;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.LoginService.Grpc;
using Wakaikami.LoginService.Grpc.Interfaces;
using Wakaikami.LoginService.Tests.TestSupport;
using Wakaikami.Networking.Grpc.Enums;
using Wakaikami.Networking.Grpc.Messages.WorldPush;

namespace Wakaikami.LoginService.Tests.Grpc;

public sealed class WorldPushServiceTests
{
    private readonly IWorldPushHub _hub = Substitute.For<IWorldPushHub>();
    private readonly IWorldServerManager _worldServers = Substitute.For<IWorldServerManager>();
    private readonly IAccountPresence _presence = Substitute.For<IAccountPresence>();

    private WorldPushService CreateService() => new(_hub, _worldServers, _presence, NullLogger<WorldPushService>.Instance);

    private void GivenKnownWorld(sbyte worldId, WorldServer server) =>
        _worldServers
            .GetWorldById(worldId, out Arg.Any<WorldServer?>())
            .Returns(call =>
            {
                call[1] = server;
                return true;
            });

    [Fact]
    public async Task SubscribeWritesHeartbeatFirst()
    {
        _hub.Subscribe(1, Arg.Any<CancellationToken>()).Returns(EmptyStream());
        GivenKnownWorld(1, new WorldServer(NullLogger<WorldServer>.Instance) { Info = Fakers.World(id: 1) });

        var writer = new TestServerStreamWriter<WorldPush>();
        await CreateService().Subscribe(new WorldSubscribeRequest { WorldId = 1 }, writer, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.NotEmpty(writer.Written);
        Assert.Equal(WorldPushKind.Heartbeat, writer.Written[0].Kind);
    }

    [Fact]
    public async Task SubscribeStreamEndsMarksTheWorldDisconnected()
    {
        _hub.Subscribe(1, Arg.Any<CancellationToken>()).Returns(EmptyStream());
        var server = new WorldServer(NullLogger<WorldServer>.Instance) { Info = Fakers.World(id: 1) };
        server.RegisterViaGrpc();
        GivenKnownWorld(1, server);

        await CreateService()
            .Subscribe(
                new WorldSubscribeRequest { WorldId = 1 },
                new TestServerStreamWriter<WorldPush>(),
                GrpcTest.Context(TestContext.Current.CancellationToken)
            );

        Assert.False(server.IsReady);
    }

    [Fact]
    public async Task SubscribeStreamEndsFreesTheAccountsThatWorldHeld()
    {
        _hub.Subscribe(1, Arg.Any<CancellationToken>()).Returns(EmptyStream());
        _worldServers.GetWorldById(1, out Arg.Any<WorldServer?>()).Returns(false);
        _presence.ReleaseWorld(1).Returns([10, 11]);

        await CreateService()
            .Subscribe(
                new WorldSubscribeRequest { WorldId = 1 },
                new TestServerStreamWriter<WorldPush>(),
                GrpcTest.Context(TestContext.Current.CancellationToken)
            );

        _presence.Received(1).ReleaseWorld(1);
    }

    [Fact]
    public async Task SubscribeStreamEndsUnregistrableWorldIdReleasesNothing()
    {
        _hub.Subscribe(0, Arg.Any<CancellationToken>()).Returns(EmptyStream());
        _worldServers.GetWorldById(0, out Arg.Any<WorldServer?>()).Returns(false);

        await CreateService()
            .Subscribe(
                new WorldSubscribeRequest { WorldId = 0 },
                new TestServerStreamWriter<WorldPush>(),
                GrpcTest.Context(TestContext.Current.CancellationToken)
            );

        _presence.DidNotReceive().ReleaseWorld(Arg.Any<sbyte>());
    }

    [Fact]
    public async Task SubscribeForwardsEveryPushAfterTheHeartbeat()
    {
        var push = new WorldPush
        {
            Kind = WorldPushKind.AccountStateFailed,
            AccountStateFailed = new AccountStateFailedPush { AccountId = 5 },
        };
        _hub.Subscribe(1, Arg.Any<CancellationToken>()).Returns(StreamOf(push));
        _worldServers.GetWorldById(1, out Arg.Any<WorldServer?>()).Returns(false);

        var writer = new TestServerStreamWriter<WorldPush>();
        await CreateService().Subscribe(new WorldSubscribeRequest { WorldId = 1 }, writer, GrpcTest.Context(TestContext.Current.CancellationToken));

        Assert.Equal(2, writer.Written.Count);
        Assert.Equal(5, writer.Written[1].AccountStateFailed.AccountId);
    }

    [Fact]
    public async Task SubscribeUnknownWorldIdDoesNotThrow()
    {
        _hub.Subscribe(99, Arg.Any<CancellationToken>()).Returns(EmptyStream());
        _worldServers.GetWorldById(99, out Arg.Any<WorldServer?>()).Returns(false);

        await CreateService()
            .Subscribe(
                new WorldSubscribeRequest { WorldId = 99 },
                new TestServerStreamWriter<WorldPush>(),
                GrpcTest.Context(TestContext.Current.CancellationToken)
            );
    }

    private static async IAsyncEnumerable<WorldPush> EmptyStream()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<WorldPush> StreamOf(WorldPush push)
    {
        await Task.CompletedTask;
        yield return push;
    }
}
