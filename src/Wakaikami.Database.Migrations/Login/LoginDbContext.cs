using Microsoft.EntityFrameworkCore;
using Wakaikami.Database.Common.CustomMigrations;
using Wakaikami.Database.Login.Entities;
using Wakaikami.Database.Login.Entities.Enums;

namespace Wakaikami.Database.Migrations.Login;

public class LoginDbContext(DbContextOptions<LoginDbContext> options) : MigratableDbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountBan> AccountBans => Set<AccountBan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("account");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(256);
            entity.Property(x => x.Password).HasMaxLength(256);
            entity.Property(x => x.CreationIp).HasMaxLength(15);
            entity.Property(x => x.IsActivated).HasDefaultValue(value: false);
            entity.Property(x => x.LastLoginIp).HasMaxLength(15);
            entity.Property(x => x.AccountType).HasDefaultValue(AccountTypes.Player);
        });

        modelBuilder.Entity<AccountBan>(entity =>
        {
            entity.ToTable("accountBan");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ip).HasMaxLength(15);
            entity.Property(x => x.Reason).HasMaxLength(512);
        });
    }
}
