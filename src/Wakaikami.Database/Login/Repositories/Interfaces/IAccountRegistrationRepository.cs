using Wakaikami.Database.Login.Entities;
using Wakaikami.Database.Login.Repositories.Models;

namespace Wakaikami.Database.Login.Repositories.Interfaces;

public interface IAccountRegistrationRepository
{
    /// <summary>Returns <c>true</c> if an account with the given username already exists.</summary>
    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Persists a new account. The unique username index is the final guard against races.</summary>
    public Task<AccountRegistrationResult> AddAccountAsync(Account account, CancellationToken cancellationToken = default);
}
