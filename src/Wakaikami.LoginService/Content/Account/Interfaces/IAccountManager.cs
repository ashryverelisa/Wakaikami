using Wakaikami.LoginService.GameNetwork.Protocol.User;

namespace Wakaikami.LoginService.Content.Account.Interfaces;

public interface IAccountManager
{
    public Task<(int accountId, UserErrors? error)> LoginAccountAsync(
        string userName,
        string password,
        string loginIp,
        CancellationToken cancellationToken = default
    );
}
