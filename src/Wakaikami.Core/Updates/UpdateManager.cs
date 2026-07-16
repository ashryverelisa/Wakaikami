using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Core.Time;
using Wakaikami.Core.Updates.Interfaces;

namespace Wakaikami.Core.Updates;

public sealed partial class UpdateManager(
    ServerTimeProvider? serverTime = null,
    ILogger<UpdateManager>? logger = null
) : IDisposable, IShutdownHandler
{
    ShutdownOrder IShutdownHandler.Order => ShutdownOrder.Updates;

    void IShutdownHandler.Shutdown() => Dispose();

    private static readonly TimeSpan DefaultTickInterval = TimeSpan.FromMilliseconds(200);

    private readonly ServerTimeProvider _serverTime = serverTime ?? new ServerTimeProvider();
    private readonly ILogger _logger = logger ?? (ILogger)NullLogger.Instance;
    private readonly Lock _lifecycleLock = new();

    private readonly ConcurrentDictionary<IUpdatable, byte> _updates = new();
    private readonly ConcurrentDictionary<ExpireEntry, byte> _expire = new();

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private volatile bool _isDisposed;

    public void Start()
    {
        lock (_lifecycleLock)
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);

            if (_cts != null)
            {
                throw new InvalidOperationException("UpdateManager already started.");
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _loop = Task.Run(() => RunLoopAsync(token), CancellationToken.None);
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        Task? loop;

        lock (_lifecycleLock)
        {
            cts = _cts;
            loop = _loop;
            _cts = null;
            _loop = null;
        }

        if (cts == null)
        {
            return;
        }

        cts.Cancel();
        loop?.GetAwaiter().GetResult();
        cts.Dispose();
    }

    public bool Register(IUpdatable obj)
    {
        if (_isDisposed || !_updates.TryAdd(obj, 0))
        {
            return false;
        }

        if (_isDisposed)
        {
            _updates.TryRemove(obj, out _);
            return false;
        }

        return true;
    }

    public bool Unregister(IUpdatable obj) => _updates.TryRemove(obj, out _);

    /// <summary>
    /// Schedules <paramref name="onExpire"/> to run once when the server clock reaches
    /// <paramref name="expireAt"/>. Dispose the returned handle to cancel before it fires
    /// (e.g. when the owning object is torn down early); disposing after it has fired is a no-op.
    /// </summary>
    public IDisposable ScheduleExpiry(DateTime expireAt, Action onExpire)
    {
        var entry = new ExpireEntry(this, expireAt, onExpire);

        if (!_isDisposed)
        {
            _expire.TryAdd(entry, 0);

            if (_isDisposed)
            {
                _expire.TryRemove(entry, out _);
            }
        }

        return entry;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        _isDisposed = true;

        Stop();

        foreach (var entry in _updates.Keys)
        {
            try
            {
                entry.Dispose();
            }
            catch (Exception ex)
            {
                LogDisposeFailed(ex, entry.GetType().Name);
            }
        }

        _updates.Clear();
        _expire.Clear();
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(DefaultTickInterval, _serverTime);

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    _serverTime.Tick();
                    var now = _serverTime.GetUtcNow().UtcDateTime;

                    TickUpdates(now);
                    TickExpire(now);
                }
                catch (Exception ex)
                {
                    LogLoopError(ex);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private void TickUpdates(DateTime now)
    {
        foreach (var (entry, _) in _updates)
        {
            try
            {
                if (entry.IsDisposed)
                {
                    _updates.TryRemove(entry, out _);
                    continue;
                }

                entry.Tick(now);
            }
            catch (Exception ex)
            {
                LogTickFailed(ex, entry.GetType().Name);
            }
        }
    }

    private void TickExpire(DateTime now)
    {
        foreach (var (entry, _) in _expire)
        {
            if (now < entry.ExpireAt)
            {
                continue;
            }

            if (_expire.TryRemove(entry, out _))
            {
                try
                {
                    entry.OnExpire();
                }
                catch (Exception ex)
                {
                    LogExpiryCallbackFailed(ex);
                }
            }
        }
    }

    private sealed class ExpireEntry(UpdateManager owner, DateTime expireAt, Action onExpire)
        : IDisposable
    {
        public DateTime ExpireAt { get; } = expireAt;
        public Action OnExpire { get; } = onExpire;

        public void Dispose() => owner._expire.TryRemove(this, out _);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Error disposing {Type}")]
    private partial void LogDisposeFailed(Exception exception, string? type);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "UpdateManager loop error")]
    private partial void LogLoopError(Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error updating {Type}")]
    private partial void LogTickFailed(Exception exception, string type);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "Error in expiry callback")]
    private partial void LogExpiryCallbackFailed(Exception exception);
}
