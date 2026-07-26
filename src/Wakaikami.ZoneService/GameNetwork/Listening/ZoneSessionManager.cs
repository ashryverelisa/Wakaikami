using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Wakaikami.Networking.Listening;
using Wakaikami.Networking.Session;
using Wakaikami.ZoneService.Content.Entity.Character;
using Wakaikami.ZoneService.Diagnostics;
using Wakaikami.ZoneService.GameNetwork.Listening.Interfaces;
using Wakaikami.ZoneService.Grpc.Handlers.Servers;

namespace Wakaikami.ZoneService.GameNetwork.Listening;

public sealed class ZoneSessionManager(ushort maxConnection, ISh03ClusterHandler sh03Cluster, ILogger<ZoneSessionManager> logger)
    : SessionManagerBase<ZoneSession>(maxConnection, logger),
        IZoneSessionManager
{
    public int SessionCount => _sessionsBySessionId.Count;

    public int OnlineCharacterCount
    {
        get
        {
            var count = 0;
            foreach (var session in _sessionsBySessionId.Values)
            {
                if (session.Character is not null)
                    count++;
            }
            return count;
        }
    }

    protected override TimeSpan TickInterval => _housekeepingInterval;

    private readonly ConcurrentDictionary<ushort, ZoneSession> _sessionsBySessionId = new();
    private static readonly TimeSpan _connectTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan _syncTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan _housekeepingInterval = TimeSpan.FromSeconds(30);

    public IReadOnlyList<ZoneCharacter> GetOnlineCharacters()
    {
        if (_sessionsBySessionId.IsEmpty)
            return [];

        var result = new List<ZoneCharacter>(_sessionsBySessionId.Count);
        foreach (var session in _sessionsBySessionId.Values)
        {
            if (session.Character is { } character)
                result.Add(character);
        }
        return result;
    }

    public override void OnUpdate(DateTime now)
    {
        foreach (var session in SessionList.Keys)
        {
            if (session.Character is null)
            {
                if (now - session.ConnectionInfo.CreateTime > _connectTimeout)
                {
                    ZoneMetrics.SessionTimedOut("connect_timeout");
                    session.Dispose();
                }

                continue;
            }

            if (now - session.ConnectionInfo.LastSync > _syncTimeout)
            {
                ZoneMetrics.SessionTimedOut("sync_timeout");
                session.Dispose();
            }
            else
            {
                session.SendHeartBeat();
            }
        }
    }

    public bool TryGetSession(ushort sessionId, out FiestaSession? session)
    {
        var found = GetSessionBySessionId(sessionId, out var zoneSession);
        session = zoneSession;
        return found;
    }

    public bool AddSession(ZoneSession session, ushort sessionId)
    {
        if (!_sessionsBySessionId.TryAdd(sessionId, session))
            return false;

        session.RegisteredSessionId = sessionId;

        var cutState = 0;

        session.OnDispose += (_, _) => NotifyAccountCut();

        if (session.IsDisposed)
        {
            _sessionsBySessionId.TryRemove(sessionId, out _);
            NotifyAccountCut();
            return false;
        }

        return true;

        void NotifyAccountCut()
        {
            if (!session.Logout && Interlocked.Exchange(ref cutState, 1) == 0)
                sh03Cluster.NcAccountSessionCut(sessionId);
        }
    }

    public override bool RemoveSession(ZoneSession? session)
    {
        if (session == null || !base.RemoveSession(session))
            return false;

        _sessionsBySessionId.TryRemove(session.RegisteredSessionId, out _);
        return true;
    }

    public bool GetSessionBySessionId(ushort sessionId, out ZoneSession? session)
    {
        return _sessionsBySessionId.TryGetValue(sessionId, out session);
    }
}
