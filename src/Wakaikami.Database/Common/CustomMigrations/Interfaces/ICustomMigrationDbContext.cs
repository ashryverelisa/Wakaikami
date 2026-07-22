using Microsoft.EntityFrameworkCore;

namespace Wakaikami.Database.Common.CustomMigrations.Interfaces;

public interface ICustomMigrationDbContext
{
    public DbSet<CustomMigrationHistory> CustomMigrationHistory { get; }
}
