using Grpc.Core;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.LoginService.Grpc.Interfaces;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Enums;
using Wakaikami.Networking.Grpc.Messages.WorldPush;

namespace Wakaikami.LoginService.Grpc;

public sealed class WorldPushService(IWorldPushHub hub, IWorldServerManager worldServers) : WorldPushStream.WorldPushStreamBase
{
    public override Task Subscribe(WorldSubscribeRequest request, IServerStreamWriter<WorldPush> responseStream, ServerCallContext context) =>
        PushStreamServer.PumpAsync(
            hub.Subscribe(request.WorldId, context.CancellationToken),
            responseStream,
            initialPush: new WorldPush { Kind = WorldPushKind.Heartbeat },
            onStreamEnded: () =>
            {
                if (worldServers.GetWorldById((sbyte)request.WorldId, out var server))
                    server.DisconnectGrpc();
            },
            context.CancellationToken
        );
}
