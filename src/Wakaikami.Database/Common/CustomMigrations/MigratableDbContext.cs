using Microsoft.EntityFrameworkCore;
using Wakaikami.Database.Common.CustomMigrations.Interfaces;

namespace Wakaikami.Database.Common.CustomMigrations;

public abstract class MigratableDbContext(DbContextOptions options) : DbContext(options), ICustomMigrationDbContext
{
    public DbSet<CustomMigrationHistory> CustomMigrationHistory => Set<CustomMigrationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomMigrationHistory>(entity =>
        {
            entity.ToTable("customMigrationHistory");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MigrationName).IsUnique();
            entity.Property(x => x.MigrationName).HasMaxLength(256);
        });
    }
}
