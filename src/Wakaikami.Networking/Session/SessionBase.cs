using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.Extensions;
using Wakaikami.Networking.Protocol.Abstractions.Interfaces;
using Wakaikami.Networking.Session.Info;

namespace Wakaikami.Networking.Session;

public abstract partial class SessionBase : IDisposable, IAsyncDisposable
{
    private const int MinReceiveBufferSize = 4096;
    private const int SendQueueCapacity = 256;

    // The [LoggerMessage] generator binds to an ILogger *field* (SYSLIB1019); derived classes use the property.
    private readonly ILogger _logger;
    protected ILogger Logger => _logger;

    /// <summary>Raised once when the session is disposed; the sender is the session itself.</summary>
    public event EventHandler? OnDispose;

    public SessionInfo ConnectionInfo { get; }

    private int _disposed;
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    internal IPacketParser? DataParser { get; set; }

    public bool IsConnected => !IsDisposed && PSocket.Connected;

    internal Socket PSocket { get; }

    private readonly Pipe _receivePipe = new();

    private readonly Channel<PooledSegment> _sendChannel = Channel.CreateBounded<PooledSegment>(
        new BoundedChannelOptions(SendQueueCapacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait }
    );

    private readonly CancellationTokenSource _cts = new();
    private Task _loopsCompleted = Task.CompletedTask;

    // 0 = not started, 1 = loops running, 2 = disposed before Start (loops will never run).
    private int _started;

    public CancellationToken SessionToken { get; }

    protected SessionBase(Socket connectSocket, ILogger logger)
    {
        _logger = logger;
        PSocket = connectSocket;
        ConnectionInfo = new SessionInfo { RemoteEndPoint = PSocket.RemoteEndPoint?.ToString() ?? "0.0.0.0" };

        SessionToken = _cts.Token;
    }

    public void Start()
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) != 0)
            return;

        var ct = SessionToken;
        var receiveLoop = Task.Run(() => FillReceivePipeAsync(ct), ct);
        var parseLoop = Task.Run(() => ReadReceivePipeAsync(ct), ct);
        var sendLoop = Task.Run(() => SendLoopAsync(ct), ct);

        _loopsCompleted = Task.WhenAll(receiveLoop, parseLoop, sendLoop);
        _ = _loopsCompleted.ContinueWith(
            static (t, state) =>
            {
                _ = t.Exception; // observe to avoid an UnobservedTaskException if a loop faulted
                ((CancellationTokenSource)state!).Dispose();
            },
            _cts,
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default
        );
    }

    private async Task FillReceivePipeAsync(CancellationToken ct)
    {
        var writer = _receivePipe.Writer;

        try
        {
            while (!ct.IsCancellationRequested && !IsDisposed)
            {
                var memory = writer.GetMemory(MinReceiveBufferSize);

                int received;
                try
                {
                    received = await PSocket.ReceiveAsync(memory, SocketFlags.None, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException ex) when (IsExpectedDisconnect(ex))
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogReceiveFailed(ex, ConnectionInfo.RemoteEndPoint);
                    break;
                }

                if (received == 0)
                    break;

                writer.Advance(received);

                try
                {
                    var flush = await writer.FlushAsync(ct);
                    if (flush.IsCompleted)
                        break;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            try
            {
                await writer.CompleteAsync();
            }
            catch (Exception ex)
            {
                LogPipeWriterCompleteFailed(ex);
            }
        }
    }

    private async Task ReadReceivePipeAsync(CancellationToken ct)
    {
        var reader = _receivePipe.Reader;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                ReadResult result;
                try
                {
                    result = await reader.ReadAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var buffer = result.Buffer;

                if (DataParser is not null && !buffer.IsEmpty)
                {
                    try
                    {
                        var parse = await DataParser.ParseAsync(buffer, ct);
                        reader.AdvanceTo(parse.Consumed, parse.Examined);
                    }
                    catch (Exception ex)
                    {
                        LogParseFailed(ex, ConnectionInfo.RemoteEndPoint);
                        break;
                    }
                }
                else
                {
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }

                if (result.IsCompleted)
                    break;
            }
        }
        finally
        {
            try
            {
                await reader.CompleteAsync();
            }
            catch (Exception ex)
            {
                LogPipeReaderCompleteFailed(ex);
            }
            Dispose();
        }
    }

    private readonly struct PooledSegment(byte[] buffer, int length)
    {
        public byte[] Buffer { get; } = buffer;
        public int Length { get; } = length;
        public ReadOnlyMemory<byte> Memory => Buffer.AsMemory(0, Length);
    }

    public virtual void SendPacket<TPacket>(TPacket packet, bool destroy = true)
        where TPacket : IServerPacket
    {
        if (IsDisposed || !PSocket.Connected)
        {
            if (destroy)
                packet.Dispose();
            Dispose();
            return;
        }

        var size = packet.WireSize;
        var rented = ArrayPool<byte>.Shared.Rent(size);

        int written;
        try
        {
            written = packet.WriteTo(rented);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(rented);
            if (destroy)
                packet.Dispose();
            throw;
        }

        if (!_sendChannel.Writer.TryWrite(new PooledSegment(rented, written)))
        {
            ArrayPool<byte>.Shared.Return(rented);
            Dispose();
        }

        if (destroy)
            packet.Dispose();
    }

    public void SendRaw(ReadOnlySpan<byte> wireBytes)
    {
        if (IsDisposed || !PSocket.Connected)
        {
            Dispose();
            return;
        }

        var rented = ArrayPool<byte>.Shared.Rent(wireBytes.Length);
        wireBytes.CopyTo(rented);

        if (!_sendChannel.Writer.TryWrite(new PooledSegment(rented, wireBytes.Length)))
        {
            ArrayPool<byte>.Shared.Return(rented);
            Dispose();
        }
    }

    private async Task SendLoopAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var segment in _sendChannel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    var memory = segment.Memory;
                    while (memory.Length > 0)
                    {
                        int sent;
                        try
                        {
                            sent = await PSocket.SendAsync(memory, SocketFlags.None, ct);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (ObjectDisposedException)
                        {
                            return;
                        }
                        catch (SocketException ex) when (IsExpectedDisconnect(ex))
                        {
                            Dispose();
                            return;
                        }
                        catch (Exception ex)
                        {
                            LogSendFailed(ex, ConnectionInfo.RemoteEndPoint);
                            Dispose();
                            return;
                        }

                        if (sent <= 0)
                        {
                            Dispose();
                            return;
                        }

                        memory = memory[sent..];
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(segment.Buffer);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LogSendLoopCrashed(ex, ConnectionInfo.RemoteEndPoint);
            Dispose();
        }
        finally
        {
            while (_sendChannel.Reader.TryRead(out var leftover))
                ArrayPool<byte>.Shared.Return(leftover.Buffer);
        }
    }

    protected virtual void DisposeInternal() { }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            try
            {
                _cts.Cancel();
            }
            catch (ObjectDisposedException) { }
            _sendChannel.Writer.TryComplete();
            OnDispose?.Invoke(this, EventArgs.Empty);
            DisposeInternal();

            if (Interlocked.CompareExchange(ref _started, 2, 0) == 0)
            {
                _cts.Dispose();
                while (_sendChannel.Reader.TryRead(out var leftover))
                    ArrayPool<byte>.Shared.Return(leftover.Buffer);
            }
        }
        PSocket.Kill();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();

        try
        {
            await _loopsCompleted;
        }
        catch (Exception ex)
        {
            LogLoopsFaultedOnDispose(ex);
        }
        GC.SuppressFinalize(this);
    }

    private static bool IsExpectedDisconnect(SocketException ex) =>
        ex.SocketErrorCode
            is SocketError.ConnectionReset
                or SocketError.ConnectionAborted
                or SocketError.Shutdown
                or SocketError.OperationAborted
                or SocketError.Disconnecting;

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Receive failed for {RemoteEndPoint}")]
    private partial void LogReceiveFailed(Exception exception, string remoteEndPoint);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "CompleteAsync on receive pipe writer failed (expected during disconnect)")]
    private partial void LogPipeWriterCompleteFailed(Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Parse failed for {RemoteEndPoint}")]
    private partial void LogParseFailed(Exception exception, string remoteEndPoint);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "CompleteAsync on receive pipe reader failed (expected during disconnect)")]
    private partial void LogPipeReaderCompleteFailed(Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Send failed for {RemoteEndPoint}")]
    private partial void LogSendFailed(Exception exception, string remoteEndPoint);

    [LoggerMessage(EventId = 6, Level = LogLevel.Error, Message = "Send loop crashed for {RemoteEndPoint}")]
    private partial void LogSendLoopCrashed(Exception exception, string remoteEndPoint);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "One or more session loops threw during disposal (expected on forced disconnect)")]
    private partial void LogLoopsFaultedOnDispose(Exception exception);
}
