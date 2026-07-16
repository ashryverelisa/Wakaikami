using System.Collections.Frozen;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Exceptions;
using Wakaikami.Core.Database.Interfaces;

namespace Wakaikami.Core.Database;

public sealed partial class SqlDbConnectionFactory : IDbConnectionFactory, IDisposable
{
    private readonly FrozenDictionary<DatabaseType, string> _connectionStrings;
    private readonly ILogger<SqlDbConnectionFactory> _logger;

    public SqlDbConnectionFactory(
        IEnumerable<DatabaseConnectionRegistration> registrations,
        ILogger<SqlDbConnectionFactory> logger
    )
    {
        _logger = logger;

        var connectionStrings = new Dictionary<DatabaseType, string>();
        foreach (var registration in registrations)
        {
            if (!connectionStrings.TryAdd(registration.Type, registration.ConnectionString))
                LogDuplicateRegistration(registration.Type);
        }

        _connectionStrings = connectionStrings.ToFrozenDictionary();
    }

    public async ValueTask<DbConnection> OpenAsync(
        DatabaseType type,
        CancellationToken cancellationToken = default
    )
    {
        if (!_connectionStrings.TryGetValue(type, out var connectionString))
            throw new DatabaseException($"Database '{type}' is not configured.");

        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask ProbeAsync(
        DatabaseType type,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = await OpenAsync(type, cancellationToken);
        LogConnectionVerified(type);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Duplicate database registration for {DatabaseType}; ignoring"
    )]
    private partial void LogDuplicateRegistration(DatabaseType databaseType);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "{DatabaseType} database connection verified"
    )]
    private partial void LogConnectionVerified(DatabaseType databaseType);

    // Flushes the process-global SqlClient connection pools on shutdown. The Generic Host
    // disposes this singleton automatically, so no separate IShutdownHandler is needed.
    public void Dispose() => SqlConnection.ClearAllPools();
}
