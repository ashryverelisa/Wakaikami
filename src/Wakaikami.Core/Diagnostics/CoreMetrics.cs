using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Wakaikami.Core.Diagnostics;

/// <summary>
/// Instruments for the game update loop. The tick has a fixed 200 ms budget; the duration
/// histogram and overrun counter are the primary "is the server keeping up" signals.
/// </summary>
public static class CoreMetrics
{
    public const string MeterName = "Wakaikami.Core";

    private static readonly Meter _meter = new(MeterName);

    // Multiple UpdateManager instances would otherwise each publish a conflicting gauge value,
    // so all instances register a source here and a single gauge sums them.
    private static readonly ConcurrentDictionary<object, Func<int>> _updatableSources = new();

    private static readonly Histogram<double> _tickDuration = _meter.CreateHistogram<double>(
        "wakaikami.core.update.tick.duration",
        unit: "s",
        description: "Duration of one update-loop tick"
    );

    private static readonly Counter<long> _tickOverruns = _meter.CreateCounter<long>(
        "wakaikami.core.update.tick.overruns",
        unit: "{tick}",
        description: "Update-loop ticks that exceeded the tick interval budget"
    );

    private static readonly Counter<long> _tickErrors = _meter.CreateCounter<long>(
        "wakaikami.core.update.tick.errors",
        unit: "{error}",
        description: "Exceptions thrown by individual updatables during a tick"
    );

    static CoreMetrics()
    {
        _meter.CreateObservableGauge(
            "wakaikami.core.update.registered",
            static () => _updatableSources.Values.Sum(static source => source()),
            unit: "{updatable}",
            description: "Objects currently registered with the update loop"
        );
    }

    public static void RecordTick(TimeSpan duration, TimeSpan budget)
    {
        _tickDuration.Record(duration.TotalSeconds);
        if (duration > budget)
            _tickOverruns.Add(1);
    }

    public static void TickItemFaulted() => _tickErrors.Add(1);

    public static void RegisterUpdatableSource(object owner, Func<int> count) => _updatableSources.TryAdd(owner, count);

    public static void UnregisterUpdatableSource(object owner) => _updatableSources.TryRemove(owner, out _);
}
