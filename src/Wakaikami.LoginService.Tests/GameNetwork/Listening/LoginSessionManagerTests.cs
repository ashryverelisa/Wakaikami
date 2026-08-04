using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wakaikami.Content.Account;
using Wakaikami.Core.Updates;
using Wakaikami.LoginService.Content.Account;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Transfer;
using Wakaikami.LoginService.GameNetwork.Listening;
using Wakaikami.LoginService.Tests.TestSupport;

namespace Wakaikami.LoginService.Tests.GameNetwork.Listening;

public sealed class LoginSessionManagerTests : IDisposable
{
    private readonly IAccountPresence _presence = Substitute.For<IAccountPresence>();
    private readonly UpdateManager _updates = new();
    private readonly AccountTransferManager _transfers;
    private readonly TestSessions _sessions = new();
    private readonly List<IDisposable> _cleanup = [];

    public LoginSessionManagerTests()
    {
        _transfers = new AccountTransferManager(_updates, _presence);
        _cleanup.Add(_updates);
        _cleanup.Add(_sessions);
    }

    public void Dispose()
    {
        foreach (var disposable in _cleanup)
            disposable.Dispose();
    }

    private LoginSessionManager NewManager(ushort maxConnections = 4) => new(maxConnections, _presence, _transfers, NullLogger<LoginSessionManager>.Instance);

    private LoginSession NewSession() => _sessions.New();

    private LoginSession OnlineSession(LoginSessionManager manager, GameAccount account)
    {
        var session = NewSession();
        Assert.True(manager.TryAcceptConnection(session));
        Assert.True(manager.AddSession(session, account));
        return session;
    }

    private void AssertClaimKept() => _presence.DidNotReceive().ReleaseIfOwnedBy(Arg.Any<int>(), Arg.Any<PresenceOwner>());

    [Fact]
    public void TryAcceptConnectionAssignsPooledIdAndIndexesSession()
    {
        var manager = NewManager();
        var session = NewSession();

        Assert.True(manager.TryAcceptConnection(session));

        Assert.NotEqual(0, session.SessionId);
        Assert.True(manager.GetSessionById(session.SessionId, out var found));
        Assert.Same(session, found);
    }

    [Fact]
    public void TryAcceptConnectionOverCapacityRejectsAndReleasesId()
    {
        var manager = NewManager(maxConnections: 1);
        var first = NewSession();
        var second = NewSession();

        Assert.True(manager.TryAcceptConnection(first));
        Assert.False(manager.TryAcceptConnection(second));

        Assert.False(manager.GetSessionById(second.SessionId, out _));

        Assert.True(manager.RemoveSession(first));
        Assert.True(manager.TryAcceptConnection(NewSession()));
    }

    [Fact]
    public void AddSessionSetsAccountOnSessionReturnsTrue()
    {
        var manager = NewManager();
        var account = Fakers.Account();

        var session = OnlineSession(manager, account);

        Assert.Same(account, session.Account);
        Assert.Equal(1, manager.OnlineCount);
    }

    [Fact]
    public void AddSessionDuplicateNameEvictsExistingAndReturnsTrue()
    {
        var manager = NewManager();
        var account = Fakers.Account();
        var ghost = OnlineSession(manager, account);

        var replacement = OnlineSession(manager, account);

        Assert.True(ghost.IsDisposed);
        Assert.True(manager.GetSessionByName(account.Name, out var current));
        Assert.Same(replacement, current);
        Assert.Equal(1, manager.OnlineCount);
    }

    [Fact]
    public void AddSessionEvictedGhostDoesNotReleaseReplacementsClaim()
    {
        var manager = NewManager();
        var account = Fakers.Account();
        OnlineSession(manager, account);

        OnlineSession(manager, account);

        AssertClaimKept();
    }

    [Fact]
    public void AddSessionDifferentCaseNameAreDistinctAccounts()
    {
        var manager = NewManager();
        var upper = Fakers.Account();
        upper.Name = "Bob";
        var lower = Fakers.Account();
        lower.Name = "bob";

        OnlineSession(manager, upper);
        OnlineSession(manager, lower);

        Assert.True(manager.GetSessionByName("Bob", out _));
        Assert.True(manager.GetSessionByName("bob", out _));
    }

    [Fact]
    public void GetSessionByNameNonExistingReturnsFalse()
    {
        var found = NewManager().GetSessionByName("Ghost", out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void RemoveSessionReleasesIdDropsNameAndFreesTheClaim()
    {
        var manager = NewManager();
        var account = Fakers.Account();
        var session = OnlineSession(manager, account);

        Assert.True(manager.RemoveSession(session));

        Assert.False(manager.GetSessionById(session.SessionId, out _));
        Assert.False(manager.GetSessionByName(account.Name, out _));
        Assert.Equal(0, manager.OnlineCount);
        _presence.Received(1).ReleaseIfOwnedBy(account.Id, PresenceOwner.Login);
    }

    [Fact]
    public void RemoveSessionWhileTransferringKeepsPresenceClaim()
    {
        var manager = NewManager();
        var account = Fakers.Account();
        var session = OnlineSession(manager, account);
        Assert.True(_transfers.GenerateTransfer(account, session: null, out _));

        Assert.True(manager.RemoveSession(session));

        AssertClaimKept();
    }

    [Fact]
    public void RemoveSessionNeverAuthenticatedSucceedsWithoutReleasingAClaim()
    {
        var manager = NewManager();
        var session = NewSession();
        manager.TryAcceptConnection(session);

        Assert.True(manager.RemoveSession(session));

        AssertClaimKept();
    }

    [Fact]
    public void RemoveSessionNeverAcceptedReturnsFalse() => Assert.False(NewManager().RemoveSession(NewSession()));

    [Fact]
    public void RemoveSessionStaleSessionLeavesReplacementInPlace()
    {
        var manager = NewManager();
        var account = Fakers.Account();
        var ghost = OnlineSession(manager, account);
        var replacement = OnlineSession(manager, account);

        Assert.False(manager.RemoveSession(ghost));
        Assert.True(manager.GetSessionByName(account.Name, out var current));
        Assert.Same(replacement, current);
    }

    [Fact]
    public void RemoveSessionAlreadyRemovedReturnsFalse()
    {
        var manager = NewManager();
        var session = OnlineSession(manager, Fakers.Account());
        manager.RemoveSession(session);

        Assert.False(manager.RemoveSession(session));
    }

    [Fact]
    public void SessionDisposeRemovesSessionFromManager()
    {
        var manager = NewManager();
        var account = Fakers.Account();
        var session = OnlineSession(manager, account);

        session.Dispose();

        Assert.False(manager.GetSessionById(session.SessionId, out _));
        Assert.Equal(0, manager.SessionCount);
        Assert.False(manager.GetSessionByName(account.Name, out _));
        _presence.Received(1).ReleaseIfOwnedBy(account.Id, PresenceOwner.Login);
    }

    [Fact]
    public void AddSessionSessionDisposedWhileAuthenticatingRollsBackAndReturnsFalse()
    {
        var manager = NewManager();
        var account = Fakers.Account();
        var session = NewSession();
        Assert.True(manager.TryAcceptConnection(session));
        session.Dispose();

        Assert.False(manager.AddSession(session, account));

        Assert.Null(session.Account);
        Assert.False(manager.GetSessionByName(account.Name, out _));
        Assert.Equal(0, manager.OnlineCount);
    }
}
