namespace Wakaikami.Core.Time;

public static class ServerClock
{
    private static TimeProvider _provider = TimeProvider.System;

    /// <summary>Wires the ambient clock to the given provider. Intended to be called once at startup.</summary>
    public static void Use(TimeProvider provider) => _provider = provider;

    /// <summary>The current server instant in UTC. Use for timestamps, durations and timeouts.</summary>
    public static DateTime UtcNow => _provider.GetUtcNow().UtcDateTime;

    /// <summary>The current server instant in the host's local time. Use only for client-facing display.</summary>
    public static DateTime LocalNow => _provider.GetLocalNow().DateTime;
}
