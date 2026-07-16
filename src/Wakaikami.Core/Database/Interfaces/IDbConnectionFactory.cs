using System.Data.Common;
using Wakaikami.Core.Database.Enums;

namespace Wakaikami.Core.Database.Interfaces;

public interface IDbConnectionFactory
{
    public ValueTask<DbConnection> OpenAsync(
        DatabaseType type,
        CancellationToken cancellationToken = default
    );

    public ValueTask ProbeAsync(DatabaseType type, CancellationToken cancellationToken = default);
}
