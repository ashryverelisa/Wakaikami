using System.Globalization;
using System.Runtime.CompilerServices;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Hosting;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Networking.Grpc;
using Wakaikami.Networking.Grpc.Enums;
using Wakaikami.Networking.Grpc.Messages.ZonePush;
using Wakaikami.Networking.Grpc.Messages.ZoneRegistration;
using Wakaikami.ZoneService.Configuration;
using Wakaikami.ZoneService.GameNetwork.Listening;
using Wakaikami.ZoneService.GameNetwork.Listening.Interfaces;

namespace Wakaikami.ZoneService.Grpc;

public sealed partial class ZonePushSubscriber(
    ZonePushStream.ZonePushStreamClient client,
    ZoneRegistration.ZoneRegistrationClient registration,
    IZoneSessionManager sessions,
    IServerLifecycle lifecycle,
    ZoneServer zoneServer,
    IOptions<ZoneOptions> options,
    ILogger<ZonePushSubscriber> logger
) : PushSubscriberBase<ZonePush>(logger)
{
    private int ZoneId => options.Value.ZoneServerInfo.ZoneId;

    protected override string StreamDescription => $"World push stream for zone {ZoneId}";

    protected override async IAsyncEnumerable<ZonePush> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var call = client.Subscribe(new ZoneSubscribeRequest { ZoneId = ZoneId }, cancellationToken: cancellationToken);
        await foreach (var push in call.ResponseStream.ReadAllAsync(cancellationToken))
            yield return push;
    }

    protected override async Task DispatchAsync(ZonePush push, CancellationToken cancellationToken)
    {
        switch (push.Kind)
        {
            case ZonePushKind.Heartbeat:
            case ZonePushKind.Unspecified:
            default:
                LogPushReceived(push.Kind);
                break;
        }
    }

    private async Task RegisterAndSyncLoopAsync(int zoneId, CancellationToken stoppingToken)
    {
        var info = options.Value.ZoneServerInfo;
        var request = new RegisterZoneRequest
        {
            ZoneId = zoneId,
            ConnectIp = info.ConnectIp,
            ConnectPort = info.ListeningPort,
            MaxPlayer = info.MaxPlayers,
            IsTestServer = info.IsTestServer,
        };

        var reply = await RegisterUntilAcceptedAsync(
            token => registration.RegisterZoneAsync(request, cancellationToken: token).ResponseAsync,
            static r => r.Accepted,
            string.Create(CultureInfo.InvariantCulture, $"RegisterZone for zone {zoneId}"),
            stoppingToken
        );
        if (reply is null || stoppingToken.IsCancellationRequested)
            return;

        await SyncLoopAsync(
            token =>
                registration.ZoneSyncAsync(new ZoneSyncRequest { ZoneId = zoneId, NowPlayers = sessions.SessionCount }, cancellationToken: token).ResponseAsync,
            TimeSpan.FromSeconds(Math.Max(1, options.Value.WorldConnectInfo.Syncinterval)),
            string.Create(CultureInfo.InvariantCulture, $"ZoneSync for zone {zoneId}"),
            stoppingToken
        );
    }

    protected override void OnSubscriptionLive(CancellationToken connectionToken) => _ = RegisterAndSyncLoopAsync(ZoneId, connectionToken);

    protected override Task<bool> OnBeforeSubscribeAsync(CancellationToken connectionToken) =>
        GameServerBootstrapper.EnsureStartedAsync(lifecycle, zoneServer.StartListening, Logger, "Zone", connectionToken);

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "World push received: {Kind}")]
    private partial void LogPushReceived(ZonePushKind kind);
}
