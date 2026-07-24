using Wakaikami.Database.Login.Repositories.Models;

namespace Wakaikami.Web.Services.Interfaces;

public interface IAccountRegistrationService
{
    public Task<bool> IsUsernameAvailableAsync(string username, CancellationToken cancellationToken = default);
    public Task<AccountRegistrationResult> RegisterAsync(string username, string password, CancellationToken cancellationToken = default);
}
