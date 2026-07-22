using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wakaikami.Database.Migrations.Login;

/// <summary>
/// Used by 'dotnet ef' only; the migrator itself builds its options in Program.cs.
/// </summary>
public class LoginDbContextFactory : IDesignTimeDbContextFactory<LoginDbContext>
{
    public LoginDbContext CreateDbContext(string[] args)
    {
        var configuration = MigratorConfiguration.Build();
        var connectionString = MigratorConfiguration.ResolveConnectionString(configuration, "Login", requirePassword: false);

        var options = new DbContextOptionsBuilder<LoginDbContext>().UseSqlServer(connectionString).Options;

        return new LoginDbContext(options);
    }
}
