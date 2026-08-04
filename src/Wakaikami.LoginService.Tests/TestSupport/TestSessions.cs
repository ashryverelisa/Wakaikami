using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Wakaikami.LoginService.GameNetwork.Listening;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.HandlerStores.Interfaces;
using Wakaikami.Networking.HandlerStores.Registration;

namespace Wakaikami.LoginService.Tests.TestSupport;

internal sealed class TestSessions : IDisposable
{
    private readonly List<IDisposable> _cleanup = [];

    public LoginSession New()
    {
        using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var clientSide = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSide.Connect(listener.LocalEndPoint!);
        var accepted = listener.Accept();

        var store = new FiestaHandlerStore(
            Array.Empty<IGamePacketHandler>(),
            Array.Empty<GamePacketTypeBinding>(),
            NullServiceProvider.Instance,
            NullLogger<FiestaHandlerStore>.Instance
        );

        var session = new LoginSession(accepted, store, NullLogger.Instance);

        _cleanup.Add(session);
        _cleanup.Add(clientSide);
        return session;
    }

    public void Dispose()
    {
        foreach (var disposable in _cleanup)
            disposable.Dispose();
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public static readonly NullServiceProvider Instance = new();

        public object? GetService(Type serviceType) => null;
    }
}
