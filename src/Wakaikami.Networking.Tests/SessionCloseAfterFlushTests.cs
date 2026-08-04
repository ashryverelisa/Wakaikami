using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Wakaikami.Networking.Session;
using Xunit;

namespace Wakaikami.Networking.Tests;

public sealed class SessionCloseAfterFlushTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly List<IDisposable> _cleanup = [];

    public void Dispose()
    {
        foreach (var disposable in _cleanup)
            disposable.Dispose();
    }

    [Fact]
    public async Task CloseAfterFlushQueuedBytesStillReachThePeer()
    {
        var (session, peer) = ConnectedPair();
        session.Start();
        byte[] payload = [0xDE, 0xAD, 0xBE, 0xEF];

        session.SendRaw(payload);
        session.CloseAfterFlush();

        Assert.Equal(payload, await ReceiveExactlyAsync(peer, payload.Length));
    }

    [Fact]
    public async Task CloseAfterFlushTearsTheSessionDownOnceTheQueueDrained()
    {
        var (session, peer) = ConnectedPair();
        session.Start();
        byte[] payload = [1, 2, 3, 4];

        session.SendRaw(payload);
        session.CloseAfterFlush();
        await ReceiveExactlyAsync(peer, payload.Length);

        await WaitUntilAsync(() => session.IsDisposed);
        Assert.True(session.IsDisposed);
    }

    [Fact]
    public async Task CloseAfterFlushIsIdempotentAndSendsThePayloadOnce()
    {
        var (session, peer) = ConnectedPair();
        session.Start();
        byte[] payload = [9, 9];

        session.SendRaw(payload);
        session.CloseAfterFlush();
        session.CloseAfterFlush();

        Assert.Equal(payload, await ReceiveExactlyAsync(peer, payload.Length));

        await WaitUntilAsync(() => session.IsDisposed);
        Assert.Equal(0, await ReceiveOnceAsync(peer));
    }

    [Fact]
    public void CloseAfterFlushOnAnAlreadyDisposedSessionDoesNothing()
    {
        var (session, _) = ConnectedPair();
        session.Start();
        session.Dispose();

        session.CloseAfterFlush();

        Assert.True(session.IsDisposed);
        Assert.False(session.IsClosing);
    }

    private (TestSession Session, Socket Peer) ConnectedPair()
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);

        var peer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        peer.Connect(listener.LocalEndPoint!);
        var accepted = listener.Accept();

        var session = new TestSession(accepted);
        _cleanup.Add(session);
        _cleanup.Add(peer);
        return (session, peer);
    }

    private static async Task<byte[]> ReceiveExactlyAsync(Socket peer, int count)
    {
        using var cts = new CancellationTokenSource(Timeout);
        var buffer = new byte[count];
        var offset = 0;

        while (offset < count)
        {
            var read = await peer.ReceiveAsync(buffer.AsMemory(offset), SocketFlags.None, cts.Token);
            Assert.NotEqual(0, read);
            offset += read;
        }

        return buffer;
    }

    private static async Task<int> ReceiveOnceAsync(Socket peer)
    {
        using var cts = new CancellationTokenSource(Timeout);
        return await peer.ReceiveAsync(new byte[16].AsMemory(), SocketFlags.None, cts.Token);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(Timeout);
        while (!condition())
            await Task.Delay(TimeSpan.FromMilliseconds(10), cts.Token);
    }

    private sealed class TestSession(Socket socket) : SessionBase(socket, NullLogger.Instance);
}
