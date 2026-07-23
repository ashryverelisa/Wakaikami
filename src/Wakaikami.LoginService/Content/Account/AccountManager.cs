using Microsoft.Extensions.Logging;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.LoginService.Content.Account.Interfaces;
using Wakaikami.LoginService.GameNetwork.Protocol.User;

namespace Wakaikami.LoginService.Content.Account;

public sealed partial class AccountManager(IAccountRepository repository, ILogger<AccountManager> logger) : IAccountManager
{
    public Task<(int accountId, UserErrors? error)> LoginAccountAsync(
        string userName,
        string password,
        string loginIp,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Error logging in")]
    private partial void LogLoginFailed(Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error banning account")]
    private partial void LogBanFailed(Exception exception);
}
