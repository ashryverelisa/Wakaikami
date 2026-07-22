using Wakaikami.LoginService.Content.Account.Interfaces;

namespace Wakaikami.LoginService.Content.Account;

public class AccountManager : IAccountManager
{
    public Task UpdateAccountStateAsync(int accountId, bool isOnline, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
