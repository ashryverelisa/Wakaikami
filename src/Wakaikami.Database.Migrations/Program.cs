using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wakaikami.Database.Common.CustomMigrations;
using Wakaikami.Database.Common.CustomMigrations.Interfaces;
using Wakaikami.Database.Migrations;
using Wakaikami.Database.Migrations.Login;

var configuration = MigratorConfiguration.Build();

using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true).SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("Migrator");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var authConnection = MigratorConfiguration.ResolveConnectionString(configuration, "Login");

try
{
    await ApplyAsync<LoginDbContext>(authConnection, options => new LoginDbContext(options), logger, cts.Token);

    logger.MigrationsApplied();
    return 0;
}
catch (Exception ex)
{
    logger.MigrationFailed(ex);
    return 1;
}

static async Task ApplyAsync<TContext>(
    string connectionString,
    Func<DbContextOptions<TContext>, TContext> factory,
    ILogger logger,
    CancellationToken cancellationToken
)
    where TContext : DbContext, ICustomMigrationDbContext
{
    logger.ApplyingMigrations(typeof(TContext).Name);

    var options = new DbContextOptionsBuilder<TContext>().UseSqlServer(connectionString).Options;
    await using var db = factory(options);

    // 1. EF Core schema migrations (the __EFMigrationsHistory-tracked ones).
    await db.Database.MigrateAsync(cancellationToken);

    // 2. Custom data/stored-procedure migrations (customMigrationHistory-tracked).
    await new CustomMigrationRunner<TContext>().Migrate(db, logger, cancellationToken);
}
