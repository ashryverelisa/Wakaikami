using Microsoft.Extensions.Logging;

namespace Wakaikami.Database.Common;

internal static partial class LoggerMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Running migration {MigrationName}")]
    public static partial void MigrationRunning(this ILogger logger, string migrationName);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Successfully migrated {MigrationName}")]
    public static partial void MigrationSucceeded(this ILogger logger, string migrationName);
}
