using Microsoft.Extensions.Logging;

namespace Wakaikami.Database.Migrations;

internal static partial class LoggerMessages
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "All migrations applied successfully.")]
    public static partial void MigrationsApplied(this ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Applying migrations failed.")]
    public static partial void MigrationFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Applying migrations for {DbContext}...")]
    public static partial void ApplyingMigrations(this ILogger logger, string dbContext);
}
