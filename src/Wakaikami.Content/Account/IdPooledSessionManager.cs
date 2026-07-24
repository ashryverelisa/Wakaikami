using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Helpers;
using Wakaikami.Networking.Listening;

namespace Wakaikami.Content.Account;

public abstract class IdPooledSessionManager<TSession>(ushort maxConnections, ILogger logger) : SessionManagerBase<TSession>(maxConnections, logger)
    where TSession : AccountSession
{
    private readonly ConcurrentDictionary<ushort, TSession> _sessionsById = new();
    private readonly IdManager _idManager = new(1, (ushort)(maxConnections + 1));

    public int SessionCount => _sessionsById.Count;

    public override bool TryAcceptConnection(TSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!_idManager.TryGetNewId(out var sessionId))
            return false;

        session.SessionId = sessionId;
        _sessionsById[sessionId] = session;

        if (!base.TryAcceptConnection(session))
        {
            _sessionsById.TryRemove(sessionId, out _);
            _idManager.TryReleaseId(sessionId);
            return false;
        }

        return true;
    }

    public override bool RemoveSession(TSession? session)
    {
        if (session == null || !base.RemoveSession(session))
            return false;

        _sessionsById.TryRemove(session.SessionId, out _);
        _idManager.TryReleaseId(session.SessionId);
        return true;
    }

    public bool GetSessionById(ushort sessionId, [NotNullWhen(true)] out TSession? session)
    {
        return _sessionsById.TryGetValue(sessionId, out session);
    }
}
