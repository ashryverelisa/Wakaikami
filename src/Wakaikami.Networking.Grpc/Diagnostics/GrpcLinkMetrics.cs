using System.Diagnostics.Metrics;

namespace Wakaikami.Networking.Grpc.Diagnostics;

/// <summary>
/// Instruments for the inter-service push streams (World↔Login, Zone↔World). A rising drop
/// count means the service chain is flapping and is worth alerting on.
/// </summary>
public static class GrpcLinkMetrics
{
    public const string MeterName = "Wakaikami.Networking.Grpc";

    private static readonly Meter _meter = new(MeterName);

    private static readonly Counter<long> _streamConnects = _meter.CreateCounter<long>(
        "wakaikami.grpc.push.connects",
        unit: "{connection}",
        description: "Push stream subscriptions that went live"
    );

    private static readonly Counter<long> _streamDrops = _meter.CreateCounter<long>(
        "wakaikami.grpc.push.drops",
        unit: "{drop}",
        description: "Push stream subscriptions that dropped and will reconnect"
    );

    public static void StreamLive(string subscriber) => _streamConnects.Add(1, new KeyValuePair<string, object?>("subscriber", subscriber));

    public static void StreamDropped(string subscriber) => _streamDrops.Add(1, new KeyValuePair<string, object?>("subscriber", subscriber));
}
