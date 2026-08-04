using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Wakaikami.Content.Account;
using Wakaikami.WorldService.GameNetwork.Listening;
using Wakaikami.WorldService.Grpc.Handlers.Server;
using Wakaikami.WorldService.Tests.TestSupport;

namespace Wakaikami.WorldService.Tests.GameNetwork.Listening;

public sealed class WorldSessionManagerTests : IDisposable
{
    private readonly ISh03ClusterHandler _cluster = Substitute.For<ISh03ClusterHandler>();
    private readonly WorldSessionFactory _sessions = new();

    public void Dispose() => _sessions.Dispose();

    private WorldSessionManager NewManager(ushort maxConnections = 4) => new(maxConnections, _cluster, NullLogger<WorldSessionManager>.Instance);

    private WorldSession NewSession() => _sessions.New();

    private static GameAccount Account(int id, string name = "Hero") => new() { Id = id, Name = name };

    private WorldSession OnlineSession(WorldSessionManager manager, GameAccount account)
    {
        var session = NewSession();
        Assert.True(manager.TryAcceptConnection(session));
        Assert.True(manager.AddSession(session, account));
        return session;
    }

    [Fact]
    public void TryAcceptConnectionAssignsPooledIdAndIndexesSession()
    {
        var manager = NewManager();
        var session = NewSession();

        Assert.True(manager.TryAcceptConnection(session));

        Assert.NotEqual(0, session.SessionId);
        Assert.True(manager.GetSessionBySessionId(session.SessionId, out var found));
        Assert.Same(session, found);
    }

    [Fact]
    public void TryAcceptConnectionOverCapacityRejectsAndReleasesId()
    {
        var manager = NewManager(maxConnections: 1);
        var first = NewSession();

        Assert.True(manager.TryAcceptConnection(first));
        Assert.False(manager.TryAcceptConnection(NewSession()));

        Assert.True(manager.RemoveSession(first));
        Assert.True(manager.TryAcceptConnection(NewSession()));
    }

    [Fact]
    public void AddSessionSetsAccountAndIndexesById()
    {
        var manager = NewManager();
        var account = Account(7);

        var session = OnlineSession(manager, account);

        Assert.Same(account, session.Account);
        Assert.True(manager.GetAccountById(7, out var found));
        Assert.Same(session, found);
        Assert.Equal(1, manager.OnlineCount);
    }

    [Fact]
    public void AddSessionDuplicateAccountEvictsTheExistingSession()
    {
        var manager = NewManager();
        var account = Account(7);
        var ghost = OnlineSession(manager, account);

        var replacement = OnlineSession(manager, account);

        Assert.True(ghost.IsDisposed);
        Assert.True(manager.GetAccountById(7, out var current));
        Assert.Same(replacement, current);
        Assert.Equal(1, manager.OnlineCount);
    }

    [Fact]
    public void AddSessionEvictedGhostDoesNotReportTheReplacementOffline()
    {
        var manager = NewManager();
        var account = Account(7);
        OnlineSession(manager, account);

        OnlineSession(manager, account);

        _cluster.DidNotReceive().NcAccountOffline(Arg.Any<GameAccount>());
    }

    [Fact]
    public void GetAccountByIdUnknownIdReturnsFalse()
    {
        var found = NewManager().GetAccountById(99, out var session);

        Assert.False(found);
        Assert.Null(session);
    }

    [Fact]
    public void GetAccountIdsReturnsEveryOnlineAccount()
    {
        var manager = NewManager();
        OnlineSession(manager, Account(1));
        OnlineSession(manager, Account(2, "Mage"));

        Assert.Equal([1, 2], [.. manager.GetAccountIds().Order()]);
    }

    [Fact]
    public void GetAccountIdsWhenEmptyReturnsEmpty() => Assert.Empty(NewManager().GetAccountIds());

    [Fact]
    public void RemoveSessionReleasesIdDropsAccountAndReportsOffline()
    {
        var manager = NewManager();
        var account = Account(7);
        var session = OnlineSession(manager, account);

        Assert.True(manager.RemoveSession(session));

        Assert.False(manager.GetSessionBySessionId(session.SessionId, out _));
        Assert.False(manager.GetAccountById(7, out _));
        Assert.Equal(0, manager.OnlineCount);
        _cluster.Received(1).NcAccountOffline(account);
    }

    [Fact]
    public void RemoveSessionNeverAuthenticatedSucceedsWithoutReportingOffline()
    {
        var manager = NewManager();
        var session = NewSession();
        manager.TryAcceptConnection(session);

        Assert.True(manager.RemoveSession(session));

        _cluster.DidNotReceive().NcAccountOffline(Arg.Any<GameAccount>());
    }

    [Fact]
    public void RemoveSessionNeverAcceptedReturnsFalse() => Assert.False(NewManager().RemoveSession(NewSession()));

    [Fact]
    public void RemoveSessionAlreadyRemovedReturnsFalse()
    {
        var manager = NewManager();
        var session = OnlineSession(manager, Account(7));
        manager.RemoveSession(session);

        Assert.False(manager.RemoveSession(session));
    }

    [Fact]
    public void SessionDisposeRemovesSessionFromManager()
    {
        var manager = NewManager();
        var account = Account(7);
        var session = OnlineSession(manager, account);

        session.Dispose();

        Assert.False(manager.GetSessionBySessionId(session.SessionId, out _));
        Assert.False(manager.GetAccountById(7, out _));
        Assert.Equal(0, manager.SessionCount);
        _cluster.Received(1).NcAccountOffline(account);
    }
}
