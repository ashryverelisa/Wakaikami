namespace Wakaikami.LoginService.Content.Account.Interfaces;

public interface IAccountManager
{
    public Task UpdateAccountStateAsync(int accountId, bool isOnline, CancellationToken cancellationToken = default);
}
