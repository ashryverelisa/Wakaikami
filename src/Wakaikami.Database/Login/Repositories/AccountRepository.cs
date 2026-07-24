using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Database;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Interfaces;
using Wakaikami.Core.Hosting.Enums;
using Wakaikami.Database.Login.Repositories.Interfaces;
using Wakaikami.Database.Login.Repositories.Models;

namespace Wakaikami.Database.Login.Repositories;

public class AccountRepository(IDbConnectionFactory connections, ILogger<AccountRepository> logger)
    : RepositoryBase<AccountRepository>(connections, DatabaseType.Login, logger),
        IAccountRepository
{
    private const string AccountGetLoginInfo = "p_Account_GetLoginInfo";
    private const string AccountMarkLoggedIn = "p_Account_MarkLoggedIn";

    public async Task<AccountLoginRow?> GetLoginInfoAsync(string userName, CancellationToken cancellationToken = default)
    {
        await using var dbConnection = await OpenConnectionAsync(cancellationToken);

        return await dbConnection.QuerySingleOrDefaultAsync<AccountLoginRow>(
            new CommandDefinition(AccountGetLoginInfo, new { pName = userName }, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken)
        );
    }

    public async Task MarkLoggedInAsync(int accountId, string loginIp, CancellationToken cancellationToken = default)
    {
        await using var dbConnection = await OpenConnectionAsync(cancellationToken);

        await dbConnection.ExecuteAsync(
            new CommandDefinition(
                AccountMarkLoggedIn,
                new { pID = accountId, pLoginIP = loginIp },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            )
        );
    }
}
