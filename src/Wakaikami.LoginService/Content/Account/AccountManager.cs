using Microsoft.Extensions.Logging;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.Content.Account.Models;
using Wakaikami.LoginService.GameNetwork.Protocol.User;

namespace Wakaikami.LoginService.Content.Account;

public sealed partial class AccountManager(IAuthenticationService authentication, IAccountRepository repository, ILogger<AccountManager> logger)
    : IAccountManager
{
    public async Task<(int accountId, UserErrors? error)> LoginAccountAsync(
        string userName,
        string password,
        string loginIp,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var login = await authentication.AuthenticateAsync(userName, password, loginIp, cancellationToken);

            return login.Status switch
            {
                LoginStatus.Blocked => (login.AccountId, UserErrors.LoginBlocked),
                LoginStatus.AgreementMissing => (login.AccountId, UserErrors.LoginAgreementMissing),
                LoginStatus.InvalidIdOrPassword => (login.AccountId, UserErrors.LoginInvalidIdOrPw),
                LoginStatus.DatabaseError => (login.AccountId, UserErrors.LoginDatabaseError),
                LoginStatus.Success => (login.AccountId, null),
                LoginStatus.Failed => (login.AccountId, UserErrors.LoginFailed),
                _ => (login.AccountId, UserErrors.LoginDatabaseError2),
            };
        }
        catch (Exception ex)
        {
            LogLoginFailed(ex);
            return (0, UserErrors.LoginDatabaseError);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Error logging in")]
    private partial void LogLoginFailed(Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error banning account")]
    private partial void LogBanFailed(Exception exception);
}
