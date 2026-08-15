using System.Data;
using System.Data.Common;
using Dapper;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Interfaces;

namespace Wakaikami.Core.Database;

public abstract class RepositoryBase<T>(IDbConnectionFactory connections, DatabaseType databaseType, ILogger<T> logger)
    where T : RepositoryBase<T>
{
    protected IDbConnectionFactory Connections { get; } = connections;
    protected DatabaseType DatabaseType { get; } = databaseType;
    protected ILogger<T> Logger { get; } = logger;

    protected ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default) =>
        Connections.OpenAsync(DatabaseType, cancellationToken);

    private static CommandDefinition Command(string proc, DynamicParameters parameters, CancellationToken cancellationToken) =>
        new(proc, parameters, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken);

    protected async Task<int> ExecAsync(string proc, DynamicParameters parameters, CancellationToken cancellationToken)
    {
        await using var dbConnection = await OpenConnectionAsync(cancellationToken);
        return await dbConnection.ExecuteAsync(Command(proc, parameters, cancellationToken));
    }

    protected async Task<TValue?> ExecScalarAsync<TValue>(string proc, DynamicParameters parameters, CancellationToken cancellationToken)
    {
        await using var dbConnection = await OpenConnectionAsync(cancellationToken);
        return await dbConnection.ExecuteScalarAsync<TValue>(Command(proc, parameters, cancellationToken));
    }

    protected async Task<IReadOnlyList<TRow>> QueryListAsync<TRow>(string proc, DynamicParameters parameters, CancellationToken cancellationToken)
    {
        await using var dbConnection = await OpenConnectionAsync(cancellationToken);
        var rows = await dbConnection.QueryAsync<TRow>(Command(proc, parameters, cancellationToken));
        return rows.AsList();
    }

    protected async Task<TRow?> QuerySingleAsync<TRow>(string proc, DynamicParameters parameters, CancellationToken cancellationToken)
    {
        await using var dbConnection = await OpenConnectionAsync(cancellationToken);
        return await dbConnection.QueryFirstOrDefaultAsync<TRow>(Command(proc, parameters, cancellationToken));
    }

    protected async Task<TResult> WithReaderAsync<TResult>(
        string proc,
        DynamicParameters parameters,
        Func<DbDataReader, CancellationToken, Task<TResult>> read,
        CancellationToken cancellationToken
    )
    {
        await using var dbConnection = await OpenConnectionAsync(cancellationToken);
        await using var reader = await dbConnection.ExecuteReaderAsync(Command(proc, parameters, cancellationToken));
        return await read(reader, cancellationToken);
    }
}
