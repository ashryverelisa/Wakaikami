using System.Diagnostics;
using Wakaikami.Networking.Grpc.Enums;
using Wakaikami.Networking.Grpc.Messages.ZonePush;
using Wakaikami.WorldService.Grpc;

namespace Wakaikami.WorldService.Tests.Grpc;

public sealed class ZonePushHubTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

    private static Task ConsumeUntilHeartbeatAsync(ZonePushHub hub, int zoneId, TaskCompletionSource<ZonePush> sink, CancellationToken ct) =>
        Task.Run(
            async () =>
            {
                await foreach (var push in hub.Subscribe(zoneId, ct))
                {
                    if (push.Kind == ZonePushKind.Unspecified)
                        continue;
                    sink.TrySetResult(push);
                    break;
                }
            },
            ct
        );

    private static async Task WaitUntilRegisteredAsync(ZonePushHub hub, int zoneId)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!hub.Publish(zoneId, new ZonePush { Kind = ZonePushKind.Unspecified }))
        {
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"zone {zoneId} subscription was never registered");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private static TaskCompletionSource<ZonePush> Sink() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static Task<ZonePush> Delivered(TaskCompletionSource<ZonePush> sink) => sink.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);

    private static async Task DrainAsync(params Task[] consumers)
    {
        try
        {
            await Task.WhenAll(consumers).WaitAsync(_timeout, CancellationToken.None);
        }
        catch (OperationCanceledException) { }
    }

    [Fact]
    public void PublishUnknownZoneReturnsFalse()
    {
        var hub = new ZonePushHub();

        Assert.False(hub.Publish(99, new ZonePush { Kind = ZonePushKind.Heartbeat }));
    }

    [Fact]
    public void BroadcastWithoutSubscribersReturnsFalse()
    {
        var hub = new ZonePushHub();

        Assert.False(hub.Broadcast(new ZonePush { Kind = ZonePushKind.Heartbeat }));
    }

    [Fact]
    public async Task PublishDeliversToTheAddressedSubscriber()
    {
        var hub = new ZonePushHub();
        using var cts = new CancellationTokenSource(_timeout);
        var sink = Sink();

        var consume = ConsumeUntilHeartbeatAsync(hub, 7, sink, cts.Token);
        await WaitUntilRegisteredAsync(hub, 7);

        Assert.True(hub.Publish(7, new ZonePush { Kind = ZonePushKind.Heartbeat }));
        Assert.Equal(ZonePushKind.Heartbeat, (await Delivered(sink)).Kind);

        await cts.CancelAsync();
        await DrainAsync(consume);
    }

    [Fact]
    public async Task PublishDoesNotReachOtherZones()
    {
        var hub = new ZonePushHub();
        using var cts = new CancellationTokenSource(_timeout);
        var other = Sink();

        var consume = ConsumeUntilHeartbeatAsync(hub, 2, other, cts.Token);
        await WaitUntilRegisteredAsync(hub, 2);

        Assert.False(hub.Publish(1, new ZonePush { Kind = ZonePushKind.Heartbeat }));
        Assert.False(other.Task.IsCompleted);

        await cts.CancelAsync();
        await DrainAsync(consume);
    }

    [Fact]
    public async Task BroadcastReachesAllSubscribers()
    {
        var hub = new ZonePushHub();
        using var cts = new CancellationTokenSource(_timeout);
        var first = Sink();
        var second = Sink();

        var consumeFirst = ConsumeUntilHeartbeatAsync(hub, 1, first, cts.Token);
        var consumeSecond = ConsumeUntilHeartbeatAsync(hub, 2, second, cts.Token);
        await WaitUntilRegisteredAsync(hub, 1);
        await WaitUntilRegisteredAsync(hub, 2);

        Assert.True(hub.Broadcast(new ZonePush { Kind = ZonePushKind.Heartbeat }));

        Assert.Equal(ZonePushKind.Heartbeat, (await Delivered(first)).Kind);
        Assert.Equal(ZonePushKind.Heartbeat, (await Delivered(second)).Kind);

        await cts.CancelAsync();
        await DrainAsync(consumeFirst, consumeSecond);
    }

    [Fact]
    public async Task SubscribeSameZoneTwiceEndsTheStaleStream()
    {
        var hub = new ZonePushHub();
        using var cts = new CancellationTokenSource(_timeout);
        var stale = Sink();
        var fresh = Sink();

        var consumeStale = ConsumeUntilHeartbeatAsync(hub, 5, stale, cts.Token);
        await WaitUntilRegisteredAsync(hub, 5);

        var consumeFresh = ConsumeUntilHeartbeatAsync(hub, 5, fresh, cts.Token);

        await consumeStale.WaitAsync(_timeout, TestContext.Current.CancellationToken);
        Assert.False(stale.Task.IsCompleted); // it ended without ever being handed a push

        await cts.CancelAsync();
        await DrainAsync(consumeFresh);
    }
}
