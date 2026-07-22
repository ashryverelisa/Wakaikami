using System.Collections.Concurrent;
using Wakaikami.LoginService.Content.Account.Interfaces;

namespace Wakaikami.LoginService.Content.Account;

public sealed class AccountPresence : IAccountPresence
{
    private readonly ConcurrentDictionary<int, PresenceOwner> _online = new();

    public bool TryReserve(int accountId, PresenceOwner owner) => _online.TryAdd(accountId, owner);

    public void MarkOnline(int accountId, PresenceOwner owner) => _online[accountId] = owner;

    public bool ReleaseIfOwnedBy(int accountId, PresenceOwner owner) => _online.TryRemove(new KeyValuePair<int, PresenceOwner>(accountId, owner));

    public IReadOnlyList<int> ReleaseWorld(sbyte worldId)
    {
        var owner = PresenceOwner.World(worldId);
        List<int> released = [];

        foreach (var (accountId, holder) in _online)
        {
            if (holder == owner && ReleaseIfOwnedBy(accountId, owner))
                released.Add(accountId);
        }

        return released;
    }
}
