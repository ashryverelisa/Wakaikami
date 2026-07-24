using Wakaikami.Database.Login.Repositories.Models;

namespace Wakaikami.Database.Login.Repositories.Interfaces;

public interface IAccountRepository
{
    public Task<AccountLoginRow?> GetLoginInfoAsync(string userName, CancellationToken cancellationToken = default);
    public Task MarkLoggedInAsync(int accountId, string loginIp, CancellationToken cancellationToken = default);
}
