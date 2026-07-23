using Grpc.Core;
using Microsoft.Extensions.Logging;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.LoginService.Grpc.Interfaces;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Enums;
using Wakaikami.Networking.Grpc.Messages.WorldPush;

namespace Wakaikami.LoginService.Grpc;

public sealed partial class WorldPushService(IWorldPushHub hub, IWorldServerManager worldServers, IAccountPresence presence, ILogger<WorldPushService> logger)
    : WorldPushStream.WorldPushStreamBase
{
    public override Task Subscribe(WorldSubscribeRequest request, IServerStreamWriter<WorldPush> responseStream, ServerCallContext context) =>
        PushStreamServer.PumpAsync(
            hub.Subscribe(request.WorldId, context.CancellationToken),
            responseStream,
            initialPush: new WorldPush { Kind = WorldPushKind.Heartbeat },
            onStreamEnded: () => OnWorldGone(request.WorldId),
            context.CancellationToken
        );

    private void OnWorldGone(int worldId)
    {
        if (worldServers.GetWorldById((sbyte)worldId, out var server))
            server.DisconnectGrpc();

        if (!PresenceOwner.TryWorld(worldId, out var owner))
            return;

        var released = presence.ReleaseWorld(owner.WorldId);
        if (released.Count == 0)
            return;

        LogWorldPresenceReleased(worldId, released.Count);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "World {WorldId} disconnected; released {Count} account(s) it still held")]
    private partial void LogWorldPresenceReleased(int worldId, int count);
}
