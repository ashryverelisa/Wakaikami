using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Wakaikami.Database.World;

namespace Wakaikami.Database.Migrations.World;

/// <summary>
/// Used by 'dotnet ef' only; the migrator itself builds its options in Program.cs.
/// </summary>
public class WorldDbContextFactory : IDesignTimeDbContextFactory<WorldDbContext>
{
    public WorldDbContext CreateDbContext(string[] args)
    {
        var configuration = MigratorConfiguration.Build();
        var connectionString = MigratorConfiguration.ResolveConnectionString(configuration, "World", requirePassword: false);

        var options = new DbContextOptionsBuilder<WorldDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly("Wakaikami.Database.Migrations"))
            .Options;

        return new WorldDbContext(options);
    }
}
