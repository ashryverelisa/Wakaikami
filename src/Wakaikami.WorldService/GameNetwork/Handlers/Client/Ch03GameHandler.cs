using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.Networking.HandlerStores.Attributes;
using Wakaikami.Networking.HandlerTypes;
using Wakaikami.Networking.Protocol.Fiesta.Enums;
using Wakaikami.WorldService.GameNetwork.Handlers.Server;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.GameNetwork.Listening.Interfaces;
using Wakaikami.WorldService.GameNetwork.Protocol.User;
using Wakaikami.WorldService.GameNetwork.Protocol.User.Client;
using Wakaikami.WorldService.Grpc.Handlers.Server;

namespace Wakaikami.WorldService.GameNetwork.Handlers.Client;

[GameHandlerClass(FiestaHandlerType.User)]
public class Ch03GameHandler(
    IWorldSessionManager worldSessions,
    ISh03GameHandler sh03GameHandler,
    ISh03ClusterHandler sh03Cluster,
    ILogger<Ch03GameHandler> logger
)
{
    [GameHandler(GameHandler03Type.NcUserWillWorldSelectReq, typeof(NcUserWillWorldSelectReq))]
    public async Task NcUserWillWorldSelectReq(WorldSession session, NcUserWillWorldSelectReq packet)
    {
        var result = await sh03Cluster.NcAccountTransferGenReq(session.Account!);
        sh03GameHandler.NcUserWillWorldSelectAck(session, result.AuthId, result.Success);
    }

    [GameHandler(GameHandler03Type.NcUserNormalLogoutCmd, typeof(NcUserNormalLogoutCmd))]
    public async Task NcUserNormalLogoutCmd(WorldSession session, NcUserNormalLogoutCmd packet)
    {
        switch (packet.LogoutType)
        {
            case LogoutType.LtCharSelect:
                break;

            case LogoutType.LtServerSelect:
            {
                if (RequireAccount(session) is not { } account)
                    return;

                var result = await sh03Cluster.NcAccountTransferGenReq(account);
                sh03GameHandler.NcUserWillWorldSelectAck(session, result.AuthId, result.Success);
                break;
            }

            case LogoutType.LtExit:
                // Handler runs on the session parse loop; DisposeAsync would self-join. Keep sync.
                // ReSharper disable once MethodHasAsyncOverload
                session.Dispose();
                break;

            default:
                logger.UnknownLogoutType(packet.LogoutType, session.ConnectionInfo.RemoteEndPoint);
                break;
        }
    }

    [GameHandler(GameHandler03Type.NcUserAvatarListReq, typeof(NcUserAvatarListReq))]
    public async Task NcUserAvatarListReq(WorldSession session, NcUserAvatarListReq packet)
    {
        if (RequireAccount(session) is not { } account)
            return;

        sh03GameHandler.NcUserLoginWorldAck(session);
    }

    [GameHandler(GameHandler03Type.NcUserLoginWorldReq, typeof(NcUserLoginWorldReq))]
    public async Task NcUserLoginWorldReq(WorldSession session, NcUserLoginWorldReq packet)
    {
        var account = await sh03Cluster.NcAccountTransferGetReq(packet.TransferId, packet.AccountName);

        if (account is null || !worldSessions.AddSession(session, account))
        {
            FailAndClose(session, UserErrorCodes.LoginInvalidCredentials);
            return;
        }

        sh03GameHandler.NcUserLoginWorldAck(session);
    }

    private static GameAccount? RequireAccount(WorldSession session)
    {
        if (session.Account is { } account)
            return account;

        session.Dispose();
        return null;
    }

    private void FailAndClose(WorldSession session, UserErrorCodes errorCode)
    {
        sh03GameHandler.NcUserLoginWorldFailAck(session, errorCode);
        session.CloseAfterFlush();
    }
}
