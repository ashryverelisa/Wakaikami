using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Transfer;
using Wakaikami.LoginService.GameNetwork.Listening.Interfaces;

namespace Wakaikami.LoginService.GameNetwork.Listening;

public sealed class LoginSessionManager(
    ushort maxConnections,
    IAccountManager accountManager,
    AccountTransferManager transferManager,
    ILogger<LoginSessionManager> logger
) : IdPooledSessionManager<LoginSession>(maxConnections, logger), ILoginSessionManager, IGameServerModule
{
    InitialType IModule<GameInitialStage>.InitialType => InitialType.Login;
    GameInitialStage IModule<GameInitialStage>.Stage => GameInitialStage.Network;

    Task<bool> IGameServerModule.InitializeAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    private readonly ConcurrentDictionary<string, LoginSession> _sessionsByName = new(StringComparer.Ordinal);

    private const int ConnectionSyncTime = 30;
    private const int ConnectTimeOut = 5;

    protected override TimeSpan TickInterval => TimeSpan.FromSeconds(ConnectionSyncTime);

    public int OnlineCount => _sessionsByName.Count;

    public bool AddSession(LoginSession session, GameAccount account)
    {
        if (_sessionsByName.TryRemove(account.Name, out var existing))
            existing.Disconnect(true);

        if (!_sessionsByName.TryAdd(account.Name, session))
            return false;

        session.Account = account;
        return true;
    }

    public bool GetSessionByName(string name, [NotNullWhen(true)] out LoginSession? session) => _sessionsByName.TryGetValue(name, out session);

    public override bool RemoveSession(LoginSession? session)
    {
        if (session == null || !base.RemoveSession(session))
            return false;

        if (session.Account is not { } account)
            return true;

        if (!_sessionsByName.TryRemove(new KeyValuePair<string, LoginSession>(account.Name, session)))
            return true;

        if (!transferManager.IsTransfering(account))
            _ = accountManager.UpdateAccountStateAsync(account.Id, isOnline: false, CancellationToken.None);

        return true;
    }

    public override void OnUpdate(DateTime now)
    {
        foreach (var session in SessionList.Keys)
        {
            if (session.Account == null)
            {
                if (session.ConnectionInfo.OpenTime.TotalSeconds > ConnectTimeOut)
                {
                    session.Dispose();
                }
            }
            else
            {
                session.SendHeartBeat();
            }
        }
    }
}
