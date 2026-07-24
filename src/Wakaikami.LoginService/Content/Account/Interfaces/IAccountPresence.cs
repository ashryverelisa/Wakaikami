namespace Wakaikami.LoginService.Content.Account.Interfaces;

public interface IAccountPresence
{
    public bool TryReserve(int accountId, PresenceOwner owner);
    public void MarkOnline(int accountId, PresenceOwner owner);
    public bool ReleaseIfOwnedBy(int accountId, PresenceOwner owner);
    public IReadOnlyList<int> ReleaseWorld(sbyte worldId);
}
