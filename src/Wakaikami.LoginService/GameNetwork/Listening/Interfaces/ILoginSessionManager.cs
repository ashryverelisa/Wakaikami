using System.Diagnostics.CodeAnalysis;
using Wakaikami.Content.Account;

namespace Wakaikami.LoginService.GameNetwork.Listening.Interfaces;

public interface ILoginSessionManager
{
    public int OnlineCount { get; }
    public bool TryAcceptConnection(LoginSession session);
    public bool AddSession(LoginSession session, GameAccount account);
    public bool GetSessionByName(string name, [NotNullWhen(true)] out LoginSession? session);
    public bool RemoveSession(LoginSession session);
}
