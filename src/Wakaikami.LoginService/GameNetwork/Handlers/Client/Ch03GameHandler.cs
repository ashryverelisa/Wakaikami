using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wakaikami.Content.Account;
using Wakaikami.Content.World.Enums;
using Wakaikami.LoginService.Configuration;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Transfer;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.LoginService.GameNetwork.Handlers.Server;
using Wakaikami.LoginService.GameNetwork.Listening;
using Wakaikami.LoginService.GameNetwork.Listening.Interfaces;
using Wakaikami.LoginService.GameNetwork.Protocol.User;
using Wakaikami.LoginService.GameNetwork.Protocol.User.Client;
using Wakaikami.LoginService.Grpc.Interfaces;
using Wakaikami.Networking.Grpc.Enums;
using Wakaikami.Networking.Grpc.Messages.WorldPush;
using Wakaikami.Networking.HandlerStores.Attributes;
using Wakaikami.Networking.HandlerTypes;

namespace Wakaikami.LoginService.GameNetwork.Handlers.Client;

[GameHandlerClass(FiestaHandlerType.User)]
public partial class Ch03GameHandler(
    ISh03GameHandler sh03GameHandler,
    ILoginSessionManager loginSessions,
    IAccountManager accountManager,
    IAccountPresence presence,
    IWorldServerManager worldServers,
    AccountTransferManager transfers,
    IWorldPushHub worldPush,
    IOptions<LoginOptions> options,
    ILogger<Ch03GameHandler> logger
)
{
    private const int MaxFailedLoginAttempts = 5;
    private readonly string _clientVersion = options.Value.ClientVersion;

    private static bool AcceptsPlayers(GameServerState status) =>
        status is GameServerState.OK or GameServerState.Low or GameServerState.Medium or GameServerState.High;

    [GameHandler(GameHandler03Type.NcUserClientVersionCheckReq, typeof(NcUserClientVersionCheckReq))]
    public void NcUserClientVersionCheckReq(LoginSession session, NcUserClientVersionCheckReq packet)
    {
        if (!string.Equals(packet.VersionKey, _clientVersion, StringComparison.OrdinalIgnoreCase))
        {
            LogVersionMismatch(packet.VersionKey, session.ConnectionInfo.RemoteEndPoint);
            sh03GameHandler.NcUserClientWrongVersionCheckAck(session);
            return;
        }

        sh03GameHandler.NcUserClientRightVersionCheckAck(session);
    }

    [GameHandler(GameHandler03Type.NcUserUsLoginReq, typeof(NcUserUsLoginReq))]
    public async Task NcUserUsLoginReq(LoginSession session, NcUserUsLoginReq packet)
    {
        if (!EnsureNotAuthenticated(session))
            return;

        var (accountId, error) = await accountManager.LoginAccountAsync(
            packet.Username,
            packet.PasswordHash,
            session.ConnectionInfo.GetIp(),
            session.SessionToken
        );

        if (session.IsDisposed)
        {
            LogSessionGoneDuringLogin(packet.Username);
            return;
        }

        if (error is not null)
        {
            FailLogin(session, packet.Username, error.Value);
            return;
        }

        if (!presence.TryReserve(accountId, PresenceOwner.Login))
        {
            EvictAccount(packet.Username, accountId);
            FailLogin(session, packet.Username, UserErrors.LoginFailed);
            return;
        }

        var account = new GameAccount { Id = accountId, Name = packet.Username };

        if (!loginSessions.AddSession(session, account))
        {
            presence.ReleaseIfOwnedBy(accountId, PresenceOwner.Login);
            FailLogin(session, packet.Username, UserErrors.LoginFailed);
            return;
        }

        LogLoginSucceeded(packet.Username, accountId, session.ConnectionInfo.RemoteEndPoint);
        sh03GameHandler.NcUserLoginAck(session);
    }

    [GameHandler(GameHandler03Type.NcUserWorldStatusReq, typeof(NcUserWorldStatusReq))]
    public void NcUserWorldStatusReq(LoginSession session, NcUserWorldStatusReq packet)
    {
        if (!TryGetAccount(session, out _))
            return;

        sh03GameHandler.NcUserWorldStatusAck(session);
    }

    [GameHandler(GameHandler03Type.NcUserXTrapReq, typeof(NcUserXTrapReq))]
    public void NcUserXTrapReq(LoginSession session, NcUserXTrapReq packet)
    {
        //TODO: Add some logic for XTrapKey
        sh03GameHandler.NcUserXTrapAck(session);
    }

    [GameHandler(GameHandler03Type.NcUserWorldSelectReq, typeof(NcUserWorldSelectReq))]
    public void NcUserWorldSelectReq(LoginSession session, NcUserWorldSelectReq packet)
    {
        if (!TryGetAccount(session, out var account))
            return;

        if (!worldServers.GetWorldById(packet.WorldId, out var server))
        {
            LogUnknownWorld(packet.WorldId, account.Name);
            sh03GameHandler.NcUserLoginFailAck(session, UserErrors.LoginUnkownError);
            session.Dispose();
            return;
        }

        if (!AcceptsPlayers(server.Status))
        {
            sh03GameHandler.NcUserWorldSelectAck(session, Guid.Empty, server, server.Status);
            return;
        }

        if (!transfers.GenerateTransfer(account, session, out var transfer))
        {
            LogTransferRejected(account.Name, packet.WorldId);
            sh03GameHandler.NcUserLoginFailAck(session, UserErrors.LoginUnkownError);
            return;
        }

        sh03GameHandler.NcUserWorldSelectAck(session, transfer.AuthId, server, GameServerState.OK);
    }

    [GameHandler(GameHandler03Type.NcUserLoginWithOtpReq, typeof(NcUserLoginWithOptReq))]
    public void NcUserLoginWithOtpReq(LoginSession session, NcUserLoginWithOptReq packet)
    {
        if (!EnsureNotAuthenticated(session))
            return;

        if (!transfers.FinishTransfer(packet.Id, out var transfer))
        {
            LogUnknownTransfer(packet.Id, session.ConnectionInfo.RemoteEndPoint);
            session.Dispose();
            return;
        }

        if (!loginSessions.AddSession(session, transfer.Account))
        {
            session.Disconnect(notifyPeer: true);
            return;
        }

        presence.MarkOnline(transfer.Account.Id, PresenceOwner.Login);
        sh03GameHandler.NcUserLoginAck(session);
    }

    private static bool TryGetAccount(LoginSession session, [NotNullWhen(true)] out GameAccount? account)
    {
        account = session.Account;
        if (account is not null)
            return true;

        session.Dispose();
        return false;
    }

    private bool EnsureNotAuthenticated(LoginSession session)
    {
        if (session.Account is null)
            return true;

        LogRepeatedLogin(session.Account.Name, session.ConnectionInfo.RemoteEndPoint);
        session.Dispose();
        return false;
    }

    private void FailLogin(LoginSession session, string username, UserErrors error)
    {
        sh03GameHandler.NcUserLoginFailAck(session, error);

        session.FailedLoginAttempts++;
        LogLoginFailed(username, session.ConnectionInfo.RemoteEndPoint, error, session.FailedLoginAttempts);

        if (session.FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LogTooManyAttempts(username, session.ConnectionInfo.RemoteEndPoint, session.FailedLoginAttempts);
            session.Dispose();
        }
    }

    private void EvictAccount(string username, int accountId)
    {
        LogEvictingAccount(username, accountId);

        if (loginSessions.GetSessionByName(username, out var existingSession))
        {
            existingSession.Disconnect(notifyPeer: true);
        }

        worldPush.Broadcast(
            new WorldPush
            {
                Kind = WorldPushKind.AccountStateFailed,
                AccountStateFailed = new AccountStateFailedPush { AccountId = accountId },
            }
        );
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Client version mismatch from {RemoteEndPoint}: {ClientVersion}")]
    private partial void LogVersionMismatch(string? clientVersion, string remoteEndPoint);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Login for {Username} from {RemoteEndPoint} rejected with {Error} (attempt {Attempts})"
    )]
    private partial void LogLoginFailed(string username, string remoteEndPoint, UserErrors error, int attempts);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Dropping {RemoteEndPoint} after {Attempts} failed logins, last for {Username}")]
    private partial void LogTooManyAttempts(string username, string remoteEndPoint, int attempts);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Login for {Username} succeeded: account {AccountId} from {RemoteEndPoint}")]
    private partial void LogLoginSucceeded(string username, int accountId, string remoteEndPoint);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Session for {Username} was gone before its credentials check finished")]
    private partial void LogSessionGoneDuringLogin(string username);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Repeated login on an authenticated session for {Username} from {RemoteEndPoint}; dropping it"
    )]
    private partial void LogRepeatedLogin(string username, string remoteEndPoint);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Account {AccountId} ({Username}) is in use; evicting the holder")]
    private partial void LogEvictingAccount(string username, int accountId);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "{Username} selected unknown world {WorldId}")]
    private partial void LogUnknownWorld(sbyte worldId, string username);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "No transfer could be minted for {Username} to world {WorldId}")]
    private partial void LogTransferRejected(string username, sbyte worldId);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Unknown or expired transfer {TransferId} offered from {RemoteEndPoint}")]
    private partial void LogUnknownTransfer(Guid transferId, string remoteEndPoint);
}
