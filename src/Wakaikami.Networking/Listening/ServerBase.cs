using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.Session;

namespace Wakaikami.Networking.Listening;

public abstract class ServerBase<TSession>(ILoggerFactory loggerFactory) : IAsyncDisposable
    where TSession : SessionBase
{
    private const int DefaultBacklog = 128;

    private Socket? _baseSocket;
    private CancellationTokenSource? _acceptCts;
    private Task? _acceptLoop;
    private readonly ILogger _logger = loggerFactory.CreateLogger<ServerBase<TSession>>();
    private bool _disposed;

    public bool Listen(int port, int backlog = DefaultBacklog)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var bindIp = new IPEndPoint(IPAddress.Any, port);
        _baseSocket = new Socket(bindIp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            _baseSocket.Bind(bindIp);
        }
        catch (SocketException ex) when (ex.SocketErrorCode is SocketError.AddressAlreadyInUse)
        {
            _baseSocket.Dispose();
            _baseSocket = null;
            return false;
        }

        _baseSocket.Listen(backlog);

        _acceptCts = new CancellationTokenSource();
        var listener = _baseSocket;
        var token = _acceptCts.Token;
        _acceptLoop = Task.Run(() => AcceptLoopAsync(listener, token), token);

        _logger.ServerListening(bindIp);
        return true;
    }

    public void Stop()
    {
        CancelCts();
        _baseSocket?.Dispose();
        _baseSocket = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (_acceptCts is { } cts)
        {
            await cts.CancelAsync();
            cts.Dispose();
            _acceptCts = null;
        }

        if (_acceptLoop is { } loop)
        {
            try
            {
                await loop;
            }
            catch (OperationCanceledException) { }

            _acceptLoop = null;
        }

        _baseSocket?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void CancelCts()
    {
        if (_acceptCts is not { } cts)
            return;
        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException) { }

        cts.Dispose();
        _acceptCts = null;
    }

    private async Task AcceptLoopAsync(Socket listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            Socket accepted;
            try
            {
                accepted = await listener.AcceptAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                // Stop()/DisposeAsync disposed the listener while an accept was pending.
                return;
            }
            catch (SocketException ex)
            {
                _logger.ServerAcceptFailed(ex);
                continue;
            }

            TSession? session = null;
            try
            {
                session = CreateSession(accepted);
                if (AcceptSession(session))
                {
                    session.Start();
                    OnSessionStarted(session);
                }
                else
                {
                    session.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.ServerSessionCreateFailed(ex);
                try
                {
                    if (session is not null)
                        session.Dispose();
                    else
                        accepted.Close();
                }
                catch (Exception closeEx)
                {
                    _logger.ServerSocketCloseFailed(closeEx);
                }
            }
        }
    }

    protected abstract TSession CreateSession(Socket socket);
    protected abstract bool AcceptSession(TSession session);

    /// <summary>Called after an accepted session's I/O loops are running (e.g. to send the handshake).</summary>
    protected virtual void OnSessionStarted(TSession session) { }
}
