using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.LoginService.Configuration;
using Wakaikami.LoginService.GameNetwork.Listening.Interfaces;
using Wakaikami.Networking.HandlerStores;
using Wakaikami.Networking.Listening;

namespace Wakaikami.LoginService.GameNetwork.Listening;

public sealed partial class LoginServer(
    ILoginSessionManager loginSessionManager,
    FiestaHandlerStore fiestaStore,
    IOptions<LoginOptions> options,
    ILogger<LoginServer> logger,
    ILoggerFactory loggerFactory
) : FiestaServer<LoginSession>(loggerFactory), IGameServerModule, IShutdownHandler
{
    private readonly ILogger _sessionLogger = loggerFactory.CreateLogger("LoginSession");
    private readonly ushort _port = options.Value.Info.GameServerPort;

    InitialType IModule<GameInitialStage>.InitialType => InitialType.Login;
    GameInitialStage IModule<GameInitialStage>.Stage => GameInitialStage.Network;
    ShutdownOrder IShutdownHandler.Order => ShutdownOrder.Network;

    Task<bool> IGameServerModule.InitializeAsync(CancellationToken cancellationToken)
    {
        if (!Listen(_port))
        {
            LogListenFailed(logger, _port);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    protected override LoginSession CreateSession(Socket socket) => new(socket, fiestaStore, _sessionLogger);

    protected override bool AcceptSession(LoginSession session) => base.AcceptSession(session) && loginSessionManager.TryAcceptConnection(session);

    void IShutdownHandler.Shutdown() => Stop();

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Failed to listen LoginServer on port {Port}")]
    private static partial void LogListenFailed(ILogger logger, ushort port);
}
