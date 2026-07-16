namespace Wakaikami.Core.Time;

public sealed class ServerTimeProvider : TimeProvider
{
    private readonly TimeProvider _source;
    private long _utcTicks;

    public ServerTimeProvider(TimeProvider? source = null)
    {
        _source = source ?? System;
        _utcTicks = _source.GetUtcNow().UtcTicks;
    }

    public override DateTimeOffset GetUtcNow() => new(Volatile.Read(ref _utcTicks), TimeSpan.Zero);

    public override ITimer CreateTimer(
        TimerCallback callback,
        object? state,
        TimeSpan dueTime,
        TimeSpan period
    ) => _source.CreateTimer(callback, state, dueTime, period);

    public override long GetTimestamp() => _source.GetTimestamp();

    public override long TimestampFrequency => _source.TimestampFrequency;

    public override TimeZoneInfo LocalTimeZone => _source.LocalTimeZone;

    /// <summary>
    /// Advances the server clock to the source provider's current instant. Called once per tick.
    /// </summary>
    /// <returns>The elapsed time since the previous tick.</returns>
    public TimeSpan Tick()
    {
        var previous = Volatile.Read(ref _utcTicks);
        var current = _source.GetUtcNow().UtcTicks;
        Volatile.Write(ref _utcTicks, current);
        return TimeSpan.FromTicks(current - previous);
    }
}
