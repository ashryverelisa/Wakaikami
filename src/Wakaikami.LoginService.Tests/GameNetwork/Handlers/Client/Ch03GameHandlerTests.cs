using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Wakaikami.Content.Account;
using Wakaikami.Content.World.Enums;
using Wakaikami.Core.Updates;
using Wakaikami.LoginService.Configuration;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Transfer;
using Wakaikami.LoginService.Content.World;
using Wakaikami.LoginService.Content.World.Interfaces;
using Wakaikami.LoginService.GameNetwork.Handlers.Client;
using Wakaikami.LoginService.GameNetwork.Handlers.Server;
using Wakaikami.LoginService.GameNetwork.Listening;
using Wakaikami.LoginService.GameNetwork.Listening.Interfaces;
using Wakaikami.LoginService.GameNetwork.Protocol.User;
using Wakaikami.LoginService.GameNetwork.Protocol.User.Client;
using Wakaikami.LoginService.Grpc.Interfaces;
using Wakaikami.LoginService.Tests.TestSupport;
using Wakaikami.Networking.Grpc.Messages.WorldPush;

namespace Wakaikami.LoginService.Tests.GameNetwork.Handlers.Client;

public sealed class Ch03GameHandlerTests : IDisposable
{
    private const string ClientVersion = "abc123";

    private readonly ILoginSessionManager _loginSessions = Substitute.For<ILoginSessionManager>();
    private readonly IAccountManager _accountManager = Substitute.For<IAccountManager>();
    private readonly IAccountPresence _presence = Substitute.For<IAccountPresence>();
    private readonly IWorldServerManager _worldServers = Substitute.For<IWorldServerManager>();
    private readonly IWorldPushHub _worldPush = Substitute.For<IWorldPushHub>();
    private readonly AccountTransferManager _transfers = Reflection.Uninitialized<AccountTransferManager>();
    private readonly ISh03GameHandler _sh03 = Substitute.For<ISh03GameHandler>();
    private readonly LoginOptions _options = new() { ClientVersion = ClientVersion, Info = new LoginServerInfoOptions() };
    private readonly TestSessions _sessions = new();

    public void Dispose() => _sessions.Dispose();

    private Ch03GameHandler GetCh03GameHandler() => GetCh03GameHandler(_transfers);

    private Ch03GameHandler GetCh03GameHandler(AccountTransferManager transfers) =>
        new(
            _sh03,
            _loginSessions,
            _accountManager,
            _presence,
            _worldServers,
            transfers,
            _worldPush,
            Options.Create(_options),
            NullLogger<Ch03GameHandler>.Instance
        );

    private static AccountTransferManager LiveTransfers(UpdateManager updates) => new(updates, Substitute.For<IAccountPresence>());

    private LoginSession CreateSession() => _sessions.New();

    /// <summary>A session that already made it past the login step.</summary>
    private LoginSession AuthenticatedSession(GameAccount account)
    {
        var session = CreateSession();
        session.Account = account;
        return session;
    }

    private static NcUserUsLoginReq LoginPacket(string username = "Hero", string passwordHash = "md5hash") =>
        Reflection.Uninitialized<NcUserUsLoginReq>().SetBackingField("Username", username).SetBackingField("PasswordHash", passwordHash);

    private static NcUserWorldSelectReq WorldSelectPacket(sbyte worldId) =>
        Reflection.Uninitialized<NcUserWorldSelectReq>().SetBackingField("WorldId", worldId);

    private static NcUserLoginWithOptReq OtpPacket(Guid authId) => Reflection.Uninitialized<NcUserLoginWithOptReq>().SetBackingField("Id", authId);

    /// <summary>A registered world reporting <paramref name="status"/> under <paramref name="worldId"/>.</summary>
    private WorldServer GivenWorld(sbyte worldId, GameServerState status)
    {
        var server = new WorldServer(NullLogger<WorldServer>.Instance) { Info = Fakers.World(worldId), IsReady = true };
        server.UpdateStatus(status);
        _worldServers
            .GetWorldById(worldId, out Arg.Any<WorldServer?>())
            .Returns(call =>
            {
                call[1] = server;
                return true;
            });
        return server;
    }

    /// <summary>Credentials check out and the account resolves to <paramref name="accountId"/>.</summary>
    private void GivenValidCredentials(int accountId) =>
        _accountManager.LoginAccountAsync("Hero", "md5hash", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((accountId, (UserErrors?)null));

    private void GivenRejectedCredentials(UserErrors error = UserErrors.LoginInvalidIdOrPw) =>
        _accountManager.LoginAccountAsync("Hero", "md5hash", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((0, (UserErrors?)error));

    [Theory]
    [InlineData(ClientVersion)] // matching key -> right-version branch
    [InlineData("wrongkey")] // mismatch -> wrong-version branch
    public void VersionCheckSelectsBranchByClientVersion(string versionKey)
    {
        var session = CreateSession();
        var packet = Reflection.Uninitialized<NcUserClientVersionCheckReq>().SetBackingField("VersionKey", versionKey);

        GetCh03GameHandler().NcUserClientVersionCheckReq(session, packet);

        if (string.Equals(versionKey, ClientVersion, StringComparison.Ordinal))
        {
            _sh03.Received().NcUserClientRightVersionCheckAck(session);
            _sh03.DidNotReceive().NcUserClientWrongVersionCheckAck(Arg.Any<LoginSession>());
        }
        else
        {
            _sh03.Received().NcUserClientWrongVersionCheckAck(session);
            _sh03.DidNotReceive().NcUserClientRightVersionCheckAck(Arg.Any<LoginSession>());
        }
    }

    [Fact]
    public async Task LoginWhenAccountIsFreeClaimsPresenceAndAcknowledges()
    {
        var session = CreateSession();
        GivenValidCredentials(accountId: 7);
        _presence.TryReserve(7, PresenceOwner.Login).Returns(true);
        _loginSessions.AddSession(session, Arg.Any<GameAccount>()).Returns(true);

        await GetCh03GameHandler().NcUserUsLoginReq(session, LoginPacket());

        _presence.Received(1).TryReserve(7, PresenceOwner.Login);
        _sh03.Received().NcUserLoginAck(session);
    }

    [Fact]
    public async Task LoginWhenAccountAlreadyInUseFailsAndEvictsTheHolder()
    {
        var session = CreateSession();
        GivenValidCredentials(accountId: 7);
        _presence.TryReserve(7, PresenceOwner.Login).Returns(returnThis: false);

        await GetCh03GameHandler().NcUserUsLoginReq(session, LoginPacket());

        _sh03.Received().NcUserLoginFailAck(session, UserErrors.LoginFailed);
        _worldPush.Received(1).Broadcast(Arg.Is<WorldPush>(p => p.AccountStateFailed.AccountId == 7));
        _loginSessions.DidNotReceive().AddSession(Arg.Any<LoginSession>(), Arg.Any<GameAccount>());
    }

    [Fact]
    public async Task LoginWhenSessionRegistrationFailsReleasesTheClaim()
    {
        var session = CreateSession();
        GivenValidCredentials(accountId: 7);
        _presence.TryReserve(7, PresenceOwner.Login).Returns(true);
        _loginSessions.AddSession(session, Arg.Any<GameAccount>()).Returns(false);

        await GetCh03GameHandler().NcUserUsLoginReq(session, LoginPacket());

        _presence.Received(1).ReleaseIfOwnedBy(7, PresenceOwner.Login);
        _sh03.Received().NcUserLoginFailAck(session, UserErrors.LoginFailed);
    }

    [Fact]
    public async Task LoginWhenSessionRegistrationFailsDoesNotEvictAnybody()
    {
        var session = CreateSession();
        GivenValidCredentials(accountId: 7);
        _presence.TryReserve(7, PresenceOwner.Login).Returns(true);
        _loginSessions.AddSession(session, Arg.Any<GameAccount>()).Returns(false);

        await GetCh03GameHandler().NcUserUsLoginReq(session, LoginPacket());

        _worldPush.DidNotReceive().Broadcast(Arg.Any<WorldPush>());
        _loginSessions.DidNotReceive().GetSessionByName(Arg.Any<string>(), out Arg.Any<LoginSession?>());
    }

    [Fact]
    public async Task LoginWhenCredentialsRejectedNeverTouchesPresence()
    {
        var session = CreateSession();
        GivenRejectedCredentials();

        await GetCh03GameHandler().NcUserUsLoginReq(session, LoginPacket());

        _presence.DidNotReceive().TryReserve(Arg.Any<int>(), Arg.Any<PresenceOwner>());
        _sh03.Received().NcUserLoginFailAck(session, UserErrors.LoginInvalidIdOrPw);
        _worldPush.DidNotReceive().Broadcast(Arg.Any<WorldPush>());
    }

    [Fact]
    public async Task LoginOnAnAuthenticatedSessionIsRefusedAndDropsTheConnection()
    {
        var session = AuthenticatedSession(Fakers.Account(id: 7));

        await GetCh03GameHandler().NcUserUsLoginReq(session, LoginPacket());

        Assert.True(session.IsDisposed);
        await _accountManager.DidNotReceive().LoginAccountAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        _presence.DidNotReceive().TryReserve(Arg.Any<int>(), Arg.Any<PresenceOwner>());
        _loginSessions.DidNotReceive().AddSession(Arg.Any<LoginSession>(), Arg.Any<GameAccount>());
    }

    [Fact]
    public async Task LoginWhenTheConnectionDiesDuringTheCredentialsCheckClaimsNothing()
    {
        var session = CreateSession();
        _accountManager
            .LoginAccountAsync("Hero", "md5hash", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                session.Dispose();
                return (7, (UserErrors?)null);
            });

        await GetCh03GameHandler().NcUserUsLoginReq(session, LoginPacket());

        _presence.DidNotReceive().TryReserve(Arg.Any<int>(), Arg.Any<PresenceOwner>());
        _loginSessions.DidNotReceive().AddSession(Arg.Any<LoginSession>(), Arg.Any<GameAccount>());
    }

    [Fact]
    public async Task LoginAfterTooManyRejectionsDropsTheConnection()
    {
        var session = CreateSession();
        GivenRejectedCredentials();
        var handler = GetCh03GameHandler();

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await handler.NcUserUsLoginReq(session, LoginPacket());
            Assert.False(session.IsDisposed);
        }

        await handler.NcUserUsLoginReq(session, LoginPacket());

        Assert.True(session.IsDisposed);
        _sh03.Received(5).NcUserLoginFailAck(session, UserErrors.LoginInvalidIdOrPw);
    }

    [Fact]
    public void WorldStatusOnAnUnauthenticatedSessionDropsTheConnection()
    {
        var session = CreateSession();

        GetCh03GameHandler().NcUserWorldStatusReq(session, Reflection.Uninitialized<NcUserWorldStatusReq>());

        Assert.True(session.IsDisposed);
        _sh03.DidNotReceive().NcUserWorldStatusAck(Arg.Any<LoginSession>());
    }

    [Fact]
    public void WorldStatusOnAnAuthenticatedSessionAcknowledges()
    {
        var session = AuthenticatedSession(Fakers.Account(id: 7));

        GetCh03GameHandler().NcUserWorldStatusReq(session, Reflection.Uninitialized<NcUserWorldStatusReq>());

        _sh03.Received(1).NcUserWorldStatusAck(session);
    }

    [Fact]
    public void WorldSelectWhenTheWorldTakesPlayersMintsATransfer()
    {
        using var updates = new UpdateManager();
        var transfers = LiveTransfers(updates);
        var account = Fakers.Account(id: 7);
        var session = AuthenticatedSession(account);
        var server = GivenWorld(worldId: 3, GameServerState.High);

        GetCh03GameHandler(transfers).NcUserWorldSelectReq(session, WorldSelectPacket(3));

        Assert.True(transfers.IsTransfering(account));
        _sh03.Received(1).NcUserWorldSelectAck(session, Arg.Is<Guid>(id => id != Guid.Empty), server, GameServerState.OK);
    }

    [Theory]
    [InlineData(GameServerState.Full)]
    [InlineData(GameServerState.Reserved)]
    [InlineData(GameServerState.Maintenance)]
    public void WorldSelectWhenTheWorldTakesNoPlayersReportsItsStateWithoutATransfer(GameServerState status)
    {
        using var updates = new UpdateManager();
        var transfers = LiveTransfers(updates);
        var account = Fakers.Account(id: 7);
        var session = AuthenticatedSession(account);
        var server = GivenWorld(worldId: 3, status);

        GetCh03GameHandler(transfers).NcUserWorldSelectReq(session, WorldSelectPacket(3));

        Assert.False(transfers.IsTransfering(account));
        _sh03.Received(1).NcUserWorldSelectAck(session, Guid.Empty, server, server.Status);
    }

    [Fact]
    public void WorldSelectUnknownWorldFailsAndDropsTheConnection()
    {
        using var updates = new UpdateManager();
        var transfers = LiveTransfers(updates);
        var session = AuthenticatedSession(Fakers.Account(id: 7));

        GetCh03GameHandler(transfers).NcUserWorldSelectReq(session, WorldSelectPacket(3));

        _sh03.Received(1).NcUserLoginFailAck(session, UserErrors.LoginUnkownError);
        Assert.True(session.IsDisposed);
    }

    [Fact]
    public void WorldSelectWhenTheTransferIsRefusedStillAcksSoTheClientCanRecover()
    {
        using var updates = new UpdateManager();
        var transfers = LiveTransfers(updates);
        var account = Fakers.Account(id: 7);
        Assert.True(transfers.GenerateTransfer(account, session: null, out _));
        var session = AuthenticatedSession(account);
        GivenWorld(worldId: 3, GameServerState.Low);

        GetCh03GameHandler(transfers).NcUserWorldSelectReq(session, WorldSelectPacket(3));

        _sh03.Received(1).NcUserLoginFailAck(session, UserErrors.LoginUnkownError);
        _sh03.DidNotReceive().NcUserWorldSelectAck(Arg.Any<LoginSession>(), Arg.Any<Guid>(), Arg.Any<WorldServer>(), Arg.Any<GameServerState>());
    }

    [Fact]
    public void LoginWithOtpTakesTheAccountBackFromTheWorld()
    {
        using var updates = new UpdateManager();
        var transfers = LiveTransfers(updates);
        var account = Fakers.Account(id: 7);
        Assert.True(transfers.GenerateTransfer(account, session: null, out var transfer));

        var session = CreateSession();
        _loginSessions.AddSession(session, account).Returns(true);

        GetCh03GameHandler(transfers).NcUserLoginWithOtpReq(session, OtpPacket(transfer.AuthId));

        _presence.Received(1).MarkOnline(7, PresenceOwner.Login);
        _sh03.Received().NcUserLoginAck(session);
    }

    [Fact]
    public void LoginWithOtpOnAnAuthenticatedSessionIsRefusedAndDropsTheConnection()
    {
        using var updates = new UpdateManager();
        var transfers = LiveTransfers(updates);
        var account = Fakers.Account(id: 7);
        Assert.True(transfers.GenerateTransfer(account, session: null, out var transfer));

        var session = AuthenticatedSession(Fakers.Account(id: 9));

        GetCh03GameHandler(transfers).NcUserLoginWithOtpReq(session, OtpPacket(transfer.AuthId));

        Assert.True(session.IsDisposed);
        Assert.True(transfers.IsTransfering(account));
        _presence.DidNotReceive().MarkOnline(Arg.Any<int>(), Arg.Any<PresenceOwner>());
    }

    [Fact]
    public void LoginWithOtpWhenSessionRegistrationFailsDropsTheConnection()
    {
        using var updates = new UpdateManager();
        var transfers = LiveTransfers(updates);
        var account = Fakers.Account(id: 7);
        Assert.True(transfers.GenerateTransfer(account, session: null, out var transfer));

        var session = CreateSession();
        _loginSessions.AddSession(session, account).Returns(false);

        GetCh03GameHandler(transfers).NcUserLoginWithOtpReq(session, OtpPacket(transfer.AuthId));

        Assert.True(session.IsDisposed);
        _presence.DidNotReceive().MarkOnline(Arg.Any<int>(), Arg.Any<PresenceOwner>());
    }

    [Fact]
    public void LoginWithOtpWithAnUnknownTokenDropsTheConnection()
    {
        using var updates = new UpdateManager();
        var transfers = LiveTransfers(updates);
        var session = CreateSession();

        GetCh03GameHandler(transfers).NcUserLoginWithOtpReq(session, OtpPacket(Guid.NewGuid()));

        Assert.True(session.IsDisposed);
        _loginSessions.DidNotReceive().AddSession(Arg.Any<LoginSession>(), Arg.Any<GameAccount>());
    }
}
