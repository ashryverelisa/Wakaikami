using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wakaikami.Database.Common.CustomMigrations.Extensions;
using Wakaikami.Database.Common.CustomMigrations.Interfaces;

namespace Wakaikami.Database.Common.CustomMigrations;

public class CustomMigrationRunner<TDbContext>
    where TDbContext : DbContext, ICustomMigrationDbContext
{
    private static IEnumerable<ICustomMigration<TDbContext>> Migrations =>
        typeof(TDbContext)
            .Assembly.GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(ICustomMigration<TDbContext>).IsAssignableFrom(x))
            .Select(x => (ICustomMigration<TDbContext>?)Activator.CreateInstance(x))
            .WhereNotNull()
            .OrderBy(x => x.Name, StringComparer.Ordinal);

    public async Task Migrate(TDbContext db, ILogger logger, CancellationToken cancellationToken = default)
    {
        foreach (var migration in Migrations)
        {
            var isAny = await db.CustomMigrationHistory.AnyAsync(x => x.MigrationName == migration.Name, cancellationToken);

            if (isAny)
            {
                continue;
            }

            logger.MigrationRunning(migration.Name);

            var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await migration.MigrateAsync(db);

                await db.CustomMigrationHistory.AddAsync(
                    new CustomMigrationHistory { MigrationName = migration.Name, AppliedAt = DateTime.UtcNow },
                    cancellationToken
                );
                await db.SaveChangesAsync(cancellationToken);

                await transaction!.CommitAsync(cancellationToken);

                logger.MigrationSucceeded(migration.Name);
            }
            catch (Exception)
            {
                // Rollback must run to completion even when the migration was cancelled.
                await transaction!.RollbackAsync(CancellationToken.None);
                throw;
            }
            finally
            {
                await transaction!.DisposeAsync();
            }
        }
    }
}
