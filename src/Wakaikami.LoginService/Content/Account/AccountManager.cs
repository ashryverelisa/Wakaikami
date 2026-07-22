using Microsoft.Extensions.Logging;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.LoginService.Content.Account.Interfaces;

namespace Wakaikami.LoginService.Content.Account;

public sealed partial class AccountManager(IAccountRepository repository, ILogger<AccountManager> logger) : IAccountManager
{
    public Task UpdateAccountStateAsync(int accountId, bool isOnline, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Error updating account state")]
    private partial void LogUpdateAccountStateFailed(Exception exception);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error logging in")]
    private partial void LogLoginFailed(Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Error, Message = "Error banning account")]
    private partial void LogBanFailed(Exception exception);
}
