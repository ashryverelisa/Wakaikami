using Microsoft.EntityFrameworkCore;
using Wakaikami.Database.Login.Entities;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.Database.Login.Repositories.Models;

namespace Wakaikami.Database.Login.Repositories;

public sealed class AccountRegistrationRepository(LoginDbContext db) : IAccountRegistrationRepository
{
    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return db.Accounts.AnyAsync(a => a.Username == username, cancellationToken);
    }

    public async Task<AccountRegistrationResult> AddAccountAsync(Account account, CancellationToken cancellationToken = default)
    {
        if (await db.Accounts.AnyAsync(a => a.Username == account.Username, cancellationToken))
            return AccountRegistrationResult.UsernameTaken;

        db.Accounts.Add(account);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(account).State = EntityState.Detached;

            if (await db.Accounts.AnyAsync(a => a.Username == account.Username, cancellationToken))
                return AccountRegistrationResult.UsernameTaken;

            throw;
        }

        return AccountRegistrationResult.Success;
    }
}
