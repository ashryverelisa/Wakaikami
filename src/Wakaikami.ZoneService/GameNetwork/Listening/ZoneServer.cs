using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.Listening;
using Wakaikami.ZoneService.Configuration;
using Wakaikami.ZoneService.GameNetwork.Listening.Interfaces;

namespace Wakaikami.ZoneService.GameNetwork.Listening;

public partial class ZoneServer(
    IZoneSessionManager zoneSessionManager,
    FiestaHandlerStore fiestaStore,
    IOptions<ZoneOptions> options,
    ILoggerFactory loggerFactory,
    ILogger<ZoneServer> logger
) : FiestaServer<ZoneSession>(loggerFactory), IShutdownHandler
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    ShutdownOrder IShutdownHandler.Order => ShutdownOrder.Network;

    void IShutdownHandler.Shutdown() => Stop();

    public bool StartListening()
    {
        var zoneServerInfo = options.Value.ZoneServerInfo;

        if (zoneServerInfo.MaxConnection == 0)
        {
            LogInvalidMaxConnection();
            return false;
        }

        if (!Listen(zoneServerInfo.ListeningPort))
        {
            LogListenFailed(zoneServerInfo.ListeningPort);
            return false;
        }

        return true;
    }

    protected override ZoneSession CreateSession(Socket socket) => new(socket, _loggerFactory.CreateLogger<ZoneSession>(), fiestaStore);

    protected override bool AcceptSession(ZoneSession session) => base.AcceptSession(session) && zoneSessionManager.TryAcceptConnection(session);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invalid MaxConnection Count Need more Count as zero..")]
    private partial void LogInvalidMaxConnection();

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Failed to listen on port {Port}")]
    private partial void LogListenFailed(ushort port);
}
