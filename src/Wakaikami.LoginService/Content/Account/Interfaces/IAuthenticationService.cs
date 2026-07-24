using Wakaikami.LoginService.Content.Account.Models;

namespace Wakaikami.LoginService.Content.Account.Interfaces;

public interface IAuthenticationService
{
    public Task<AccountLoginResult> AuthenticateAsync(string userName, string clientMd5, string loginIp, CancellationToken cancellationToken = default);
}
