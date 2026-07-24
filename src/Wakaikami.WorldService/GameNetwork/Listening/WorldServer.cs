using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.Listening;
using Wakaikami.WorldService.Configuration;
using Wakaikami.WorldService.GameNetwork.Listening.Interfaces;

namespace Wakaikami.WorldService.GameNetwork.Listening;

public class WorldServer(
    FiestaHandlerStore fiestaStore,
    IWorldSessionManager worldSessionManager,
    IOptions<WorldOptions> options,
    ILogger<WorldServer> logger,
    ILoggerFactory loggerFactory
) : FiestaServer<WorldSession>(loggerFactory), IShutdownHandler
{
    private readonly ILogger _sessionLogger = loggerFactory.CreateLogger("WorldSession");

    ShutdownOrder IShutdownHandler.Order => ShutdownOrder.Network;

    void IShutdownHandler.Shutdown() => Stop();

    public bool Listening(int port)
    {
        if (options.Value.WorldInfo.MaxConnection == 0)
        {
            logger.InvalidMaxConnection();
            return false;
        }

        if (!Listen(port))
        {
            logger.WorldServerPortInUse(port);
            return false;
        }

        return true;
    }

    protected override WorldSession CreateSession(Socket socket) => new(socket, fiestaStore, _sessionLogger);

    protected override bool AcceptSession(WorldSession session) => base.AcceptSession(session) && worldSessionManager.TryAcceptConnection(session);
}
