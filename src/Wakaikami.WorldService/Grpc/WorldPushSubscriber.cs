using System.Globalization;
using System.Runtime.CompilerServices;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Enums;
using Wakaikami.Networking.Grpc.Messages.WorldLoginRegistration;
using Wakaikami.Networking.Grpc.Messages.WorldPush;
using Wakaikami.WorldService.Configuration;
using Wakaikami.WorldService.Core;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Listening.Interfaces;
using Wakaikami.WorldService.Grpc.Handlers.Client;
using Wakaikami.WorldService.Grpc.Handlers.Server;

namespace Wakaikami.WorldService.Grpc;

public class WorldPushSubscriber(
    WorldPushStream.WorldPushStreamClient client,
    WorldLoginRegistration.WorldLoginRegistrationClient registration,
    Ch03ClusterHandler accountHandler,
    IServerLifecycle lifecycle,
    WorldServer worldServer,
    ServerMain serverMain,
    IWorldSessionManager worldSessions,
    ISh03ClusterHandler sh03Cluster,
    IOptions<WorldOptions> options,
    ILogger<WorldPushSubscriber> logger
) : PushSubscriberBase<WorldPush>(logger)
{
    private WorldInfoOptions WorldInfo => options.Value.WorldInfo;
    private int WorldId => WorldInfo.WorldId;

    protected override string StreamDescription => $"Login push stream for world {WorldId}";

    protected override async IAsyncEnumerable<WorldPush> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var call = client.Subscribe(new WorldSubscribeRequest { WorldId = WorldId }, cancellationToken: cancellationToken);
        await foreach (var push in call.ResponseStream.ReadAllAsync(cancellationToken))
            yield return push;
    }

    // Register only AFTER the first push (the heartbeat) confirms the Login-side hub subscription is live,
    // so a duplicate-login kick can't race ahead of an unwired subscription. The register/boot/sync loop
    // then runs for this subscription's life.
    protected override void OnSubscriptionLive(CancellationToken connectionToken) => _ = RegisterBootSyncLoopAsync(WorldId, connectionToken);

    protected override Task DispatchAsync(WorldPush push, CancellationToken cancellationToken)
    {
        if (push is { Kind: WorldPushKind.AccountStateFailed, AccountStateFailed: { } failed })
            accountHandler.HandleAccountStateFailed(failed.AccountId);

        return Task.CompletedTask;
    }

    // Register (retry until accepted) → boot the game server on this world's own configured parameters →
    // keep Login's status fresh via periodic WorldSync. Tied to the subscription's lifetime.
    private async Task RegisterBootSyncLoopAsync(int worldId, CancellationToken stoppingToken)
    {
        // The world announces its own connection parameters (from config); Login builds its
        // world list dynamically from these registrations.
        var request = new RegisterWorldRequest
        {
            WorldId = worldId,
            WorldName = WorldInfo.WorldName,
            ConnectIp = WorldInfo.ConnectIp,
            ConnectPort = WorldInfo.ListeningPort,
            IsTestServer = WorldInfo.IsTestServer,
        };

        var reply = await RegisterUntilAcceptedAsync(
            token => registration.RegisterWorldAsync(request, cancellationToken: token).ResponseAsync,
            static r => r.Accepted,
            string.Create(CultureInfo.InvariantCulture, $"RegisterWorld for world {worldId}"),
            stoppingToken
        );
        if (reply is null || stoppingToken.IsCancellationRequested)
            return;

        // The TCP handshake boot path is gone, so the gRPC lifecycle always boots the game server here.
        var started = await GameServerBootstrapper.EnsureStartedAsync(
            lifecycle,
            startListening: () =>
            {
                if (!worldServer.Listening(WorldInfo.ListeningPort))
                    return false;
                serverMain.IsTestServer = WorldInfo.IsTestServer;
                return true;
            },
            Logger,
            "World",
            stoppingToken
        );
        if (!started)
            return;

        // Reconnect case: tell Login which accounts are already online on this world.
        if (worldSessions.OnlineCount > 0)
            sh03Cluster.NcAccountOnlineList(worldSessions.GetAccountIds());

        await SyncLoopAsync(
            token =>
                registration.WorldSyncAsync(new WorldSyncRequest { WorldId = worldId, State = (int)serverMain.Status }, cancellationToken: token).ResponseAsync,
            TimeSpan.FromSeconds(Math.Max(1, options.Value.WorldInfo.ConnectInfo.Syncinterval)),
            string.Create(CultureInfo.InvariantCulture, $"WorldSync for world {worldId}"),
            stoppingToken
        );
    }
}
