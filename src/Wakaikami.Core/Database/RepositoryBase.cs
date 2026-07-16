using System.Data.Common;
using Microsoft.Extensions.Logging;
using Wakaikami.Core.Database.Enums;
using Wakaikami.Core.Database.Interfaces;

namespace Wakaikami.Core.Database;

public abstract class RepositoryBase<T>(
    IDbConnectionFactory connections,
    DatabaseType databaseType,
    ILogger<T> logger
)
    where T : RepositoryBase<T>
{
    protected IDbConnectionFactory Connections { get; } = connections;
    protected DatabaseType DatabaseType { get; } = databaseType;
    protected ILogger<T> Logger { get; } = logger;

    protected ValueTask<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default
    ) => Connections.OpenAsync(DatabaseType, cancellationToken);
}
