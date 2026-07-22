using Microsoft.EntityFrameworkCore;

namespace Wakaikami.Database.Common.CustomMigrations.Interfaces;

public interface ICustomMigration<in TContext>
    where TContext : DbContext, ICustomMigrationDbContext
{
    public string Name { get; }
    public Task MigrateAsync(TContext db);
}
