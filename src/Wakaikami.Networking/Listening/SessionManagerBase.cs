using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Time;
using Wakaikami.Networking.Session;

namespace Wakaikami.Networking.Listening;

public abstract class SessionManagerBase<TSession>(ushort maxConnection, ILogger logger) : IHostedService, IDisposable
    where TSession : SessionBase
{
    public bool IsDisposed { get; private set; }

    protected virtual TimeSpan TickInterval => TimeSpan.FromSeconds(30);

    private CancellationTokenSource? _cts;
    private Task? _loop;

    protected ConcurrentDictionary<TSession, byte> SessionList { get; } = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => RunAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is null)
            return;

        try
        {
            await _cts.CancelAsync();
        }
        catch (ObjectDisposedException) { }

        if (_loop != null)
        {
            try
            {
                await _loop;
            }
            catch (OperationCanceledException) { }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TickInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (IsDisposed)
                    return;

                try
                {
                    OnUpdate(ServerClock.UtcNow);
                }
                catch (Exception ex)
                {
                    logger.SessionManagerUpdateFailed(ex, GetType().Name);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public virtual bool TryAcceptConnection(TSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (SessionList.Count >= maxConnection)
            return false;

        if (!SessionList.TryAdd(session, 0))
            return false;

        session.OnDispose += (_, _) => RemoveSession(session);
        return true;
    }

    public virtual bool RemoveSession(TSession? session)
    {
        if (session is null)
            return false;

        if (!SessionList.TryRemove(session, out _))
            return false;

        return true;
    }

    public abstract void OnUpdate(DateTime now);

    protected virtual void DisposeInternal() { }

    public void Dispose()
    {
        if (IsDisposed)
            return;
        IsDisposed = true;

        _cts?.Cancel();
        _cts?.Dispose();

        foreach (var session in SessionList.Keys)
        {
            session.Dispose();
        }
        SessionList.Clear();
        DisposeInternal();

        GC.SuppressFinalize(this);
    }
}
