using Wakaikami.Content.Account;

namespace Wakaikami.WorldService.GameNetwork.Listening.Interfaces;

public interface IWorldSessionManager
{
    public int OnlineCount { get; }
    public bool TryAcceptConnection(WorldSession session);
    public bool AddSession(WorldSession session, GameAccount account);
    public bool GetAccountById(int accountId, out WorldSession? session);
    public bool GetSessionBySessionId(ushort sessionId, out WorldSession? session);
    public int[] GetAccountIds();
    public bool RemoveSession(WorldSession session);
}
