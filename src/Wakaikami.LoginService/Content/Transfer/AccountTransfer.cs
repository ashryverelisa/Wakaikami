using Wakaikami.Content.Account;
using Wakaikami.LoginService.GameNetwork.Listening;

namespace Wakaikami.LoginService.Content.Transfer;

public sealed class AccountTransfer(GameAccount account)
{
    public Guid AuthId { get; } = Guid.NewGuid();
    public GameAccount Account { get; } = account;
    public LoginSession? Session { get; set; }

    /// <summary>Handle for the scheduled timeout; disposed by the manager when the transfer finishes early.</summary>
    public IDisposable? ExpiryHandle { get; set; }
}
