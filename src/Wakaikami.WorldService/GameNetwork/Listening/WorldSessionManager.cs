using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Wakaikami.Content.Account;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Core.Hosting.Interfaces;
using Wakaikami.WorldService.GameNetwork.Listening.Interfaces;
using Wakaikami.WorldService.Grpc.Handlers.Server;

namespace Wakaikami.WorldService.GameNetwork.Listening;

public class WorldSessionManager(ushort maxConnection, ISh03ClusterHandler sh03Cluster, ILogger<WorldSessionManager> logger)
    : IdPooledSessionManager<WorldSession>(maxConnection, logger),
        IWorldSessionManager
{
    public int OnlineCount => _sessionsByAccountId.Count;

    private readonly ConcurrentDictionary<int, WorldSession> _sessionsByAccountId = new();
    private const int ConnectionSyncTimeOut = 60;
    private const int ConnectionSyncTime = 30;
    private const int ConnectTimeOut = 5;

    protected override TimeSpan TickInterval => TimeSpan.FromSeconds(ConnectionSyncTime);

    public override void OnUpdate(DateTime now)
    {
        // Same sweep as ZoneSessionManager: kick sessions that never authenticated within ConnectTimeOut,
        // kick authenticated sessions whose heartbeat reply (Ch02 -> LastSync) is overdue, heartbeat the rest.
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
                if (session.ConnectionInfo.TimeFromLastSync.TotalSeconds > ConnectionSyncTimeOut)
                {
                    session.Dispose();
                }
                else
                {
                    session.SendHeartBeat();
                }
            }
        }
    }

    public bool AddSession(WorldSession session, GameAccount account)
    {
        session.Account = account;

        // Remove the old entry BEFORE disposing it that way RemoveSession's TryRemove
        // won't match and won't fire a spurious NC_ACCOUNT_OFFLINE for the replacement.
        if (_sessionsByAccountId.TryRemove(account.Id, out var existingSession))
        {
            existingSession.Disconnect(notifyPeer: true);
        }

        return _sessionsByAccountId.TryAdd(account.Id, session);
    }

    public bool GetAccountById(int accountId, out WorldSession? session) => _sessionsByAccountId.TryGetValue(accountId, out session);

    public bool GetSessionBySessionId(ushort sessionId, out WorldSession? session) => GetSessionById(sessionId, out session);

    public int[] GetAccountIds() => _sessionsByAccountId.Keys.ToArray();

    public override bool RemoveSession(WorldSession? session)
    {
        if (session == null || !base.RemoveSession(session))
            return false;

        if (session.Account == null)
            return true;

        _sessionsByAccountId.TryRemove(session.Account.Id, out _);
        sh03Cluster.NcAccountOffline(session.Account);
        return true;
    }
}
