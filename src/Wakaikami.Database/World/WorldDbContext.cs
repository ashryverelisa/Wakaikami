using Microsoft.EntityFrameworkCore;
using Wakaikami.Database.Common.CustomMigrations;

namespace Wakaikami.Database.World;

public class WorldDbContext(DbContextOptions<WorldDbContext> options) : MigratableDbContext(options) { }
