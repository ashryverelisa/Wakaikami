using Grpc.Core;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Enums;
using Wakaikami.Networking.Grpc.Messages.ZonePush;
using Wakaikami.WorldService.Content.Zone;
using Wakaikami.WorldService.Grpc.Interfaces;

namespace Wakaikami.WorldService.Grpc;

public sealed partial class ZonePushService(IZonePushHub hub, ZoneManager zones, ILogger<ZonePushService> logger)
    : ZonePushStream.ZonePushStreamBase
{
    public override Task Subscribe(ZoneSubscribeRequest request, IServerStreamWriter<ZonePush> responseStream, ServerCallContext context) =>
        PushStreamServer.PumpAsync(
            hub.Subscribe(request.ZoneId, context.CancellationToken),
            responseStream,
            initialPush: new ZonePush { Kind = ZonePushKind.Heartbeat },
            onStreamEnded: () => OnZoneGone(request.ZoneId),
            context.CancellationToken
        );

    private void OnZoneGone(int zoneId)
    {
        if (zones.GetZoneById((byte)zoneId, out var zone, onlyConnect: false))
            zone.DisconnectGrpc();

        LogZoneDisconnected(zoneId);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Zone {ZoneId} push stream ended; marked disconnected")]
    private partial void LogZoneDisconnected(int zoneId);
}
