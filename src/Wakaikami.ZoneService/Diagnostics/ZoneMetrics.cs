using System.Diagnostics.Metrics;

namespace Wakaikami.ZoneService.Diagnostics;

public static class ZoneMetrics
{
    public const string MeterName = "Wakaikami.ZoneService";

    private static readonly Meter _meter = new(MeterName);

    private static readonly Counter<long> _disconnects = _meter.CreateCounter<long>(
        "wakaikami.zone.disconnects",
        unit: "{session}",
        description: "Zone sessions dropped by housekeeping, by reason"
    );

    public static void SessionTimedOut(string reason) => _disconnects.Add(1, new KeyValuePair<string, object?>("reason", reason));

    /// <summary>
    /// Publishes the online-character gauge from the (single) session manager instance;
    /// safe to call once per process.
    /// </summary>
    public static void RegisterOnlineCharactersGauge(Func<int> onlineCharacters) =>
        _meter.CreateObservableGauge(
            "wakaikami.zone.characters.online",
            onlineCharacters,
            unit: "{character}",
            description: "Sessions with a character bound (players in the zone)"
        );
}
