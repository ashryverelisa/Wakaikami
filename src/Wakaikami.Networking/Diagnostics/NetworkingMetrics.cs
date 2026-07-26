using System.Diagnostics;
using System.Diagnostics.Metrics;
using Wakaikami.Networking.HandlerTypes;

namespace Wakaikami.Networking.Diagnostics;

/// <summary>
/// Instruments for the game-protocol layer: session lifecycle per manager and packet dispatch
/// per handler type/opcode. Collected by the OpenTelemetry pipeline via the <c>Wakaikami.*</c>
/// meter subscription.
/// </summary>
public static class NetworkingMetrics
{
    public const string MeterName = "Wakaikami.Networking";

    private static readonly Meter _meter = new(MeterName);

    private static readonly UpDownCounter<long> _activeSessions = _meter.CreateUpDownCounter<long>(
        "wakaikami.network.sessions.active",
        unit: "{session}",
        description: "Currently connected sessions per session manager"
    );

    private static readonly Counter<long> _rejectedConnections = _meter.CreateCounter<long>(
        "wakaikami.network.sessions.rejected",
        unit: "{connection}",
        description: "Connections rejected because the session manager was full"
    );

    private static readonly Counter<long> _handledPackets = _meter.CreateCounter<long>(
        "wakaikami.network.packets.handled",
        unit: "{packet}",
        description: "Game packets dispatched to a handler"
    );

    private static readonly Counter<long> _unhandledPackets = _meter.CreateCounter<long>(
        "wakaikami.network.packets.unhandled",
        unit: "{packet}",
        description: "Game packets without a registered handler"
    );

    private static readonly Counter<long> _packetErrors = _meter.CreateCounter<long>(
        "wakaikami.network.packets.errors",
        unit: "{packet}",
        description: "Game packets whose handler threw"
    );

    private static readonly Histogram<double> _packetDuration = _meter.CreateHistogram<double>(
        "wakaikami.network.packet.duration",
        unit: "s",
        description: "Time spent handling a game packet"
    );

    public static void SessionAccepted(string manager) => _activeSessions.Add(1, new KeyValuePair<string, object?>("manager", manager));

    public static void SessionRemoved(string manager) => _activeSessions.Add(-1, new KeyValuePair<string, object?>("manager", manager));

    public static void SessionRejected(string manager) => _rejectedConnections.Add(1, new KeyValuePair<string, object?>("manager", manager));

    public static void PacketHandled(FiestaHandlerType header, ushort opcode, double durationSeconds)
    {
        var tags = PacketTags(header, opcode);
        _handledPackets.Add(1, tags);
        _packetDuration.Record(durationSeconds, tags);
    }

    public static void PacketUnhandled(FiestaHandlerType header, ushort opcode) => _unhandledPackets.Add(1, PacketTags(header, opcode));

    public static void PacketFaulted(FiestaHandlerType header, ushort opcode) => _packetErrors.Add(1, PacketTags(header, opcode));

    private static TagList PacketTags(FiestaHandlerType header, ushort opcode) => new() { { "handler_type", header.ToString() }, { "opcode", (int)opcode } };
}
