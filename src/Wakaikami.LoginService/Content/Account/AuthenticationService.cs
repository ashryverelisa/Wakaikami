using Wakaikami.Core.Security.Interfaces;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.Database.Login.Repositories.Models;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Account.Models;

namespace Wakaikami.LoginService.Content.Account;

public sealed class AuthenticationService(IAccountRepository repository, IPasswordHasher passwordHasher) : IAuthenticationService
{
    public async Task<AccountLoginResult> AuthenticateAsync(string userName, string clientMd5, string loginIp, CancellationToken cancellationToken = default)
    {
        var account = await repository.GetLoginInfoAsync(userName, cancellationToken);

        if (account is null || !VerifyPassword(account, clientMd5))
            return new AccountLoginResult(LoginStatus.InvalidIdOrPassword, 0);

        if (!account.IsActivated)
            return new AccountLoginResult(LoginStatus.AgreementMissing, 0);

        if (account.IsBanned)
            return new AccountLoginResult(LoginStatus.Blocked, 0);

        await repository.MarkLoggedInAsync(account.Id, loginIp, cancellationToken);

        return new AccountLoginResult(LoginStatus.Success, account.Id);
    }

    private bool VerifyPassword(AccountLoginRow account, string clientMd5)
    {
        return passwordHasher.Verify(clientMd5, account.PasswordHash);
    }
}
