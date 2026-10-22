using Microsoft.EntityFrameworkCore;
using Wakaikami.Database.Common.CustomMigrations;
using Wakaikami.Database.World.Entities;

namespace Wakaikami.Database.World;

public class WorldDbContext(DbContextOptions<WorldDbContext> options) : MigratableDbContext(options)
{
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<CharacterOptions> CharacterOptions => Set<CharacterOptions>();
    public DbSet<CharacterShape> CharacterShapes => Set<CharacterShape>();
    public DbSet<CharacterStatPoints> CharacterStatPoints => Set<CharacterStatPoints>();
    public DbSet<CharacterTutorial> CharacterTutorials => Set<CharacterTutorial>();
    public DbSet<CharacterSkill> CharacterSkills => Set<CharacterSkill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Character>(entity =>
        {
            entity.ToTable("character");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(20);
            entity.Property(x => x.MapIndex).HasMaxLength(64);
            entity.Property(x => x.KqHandle).HasMaxLength(64);
            // SQL Server has no unsigned bigint; keep the column BIGINT.
            entity.Property(x => x.Exp).HasConversion<long>();
            // Deleted characters keep their row, so only live characters compete for a slot.
            entity.HasIndex(x => new { x.AccountId, x.Slot }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        modelBuilder.Entity<CharacterShape>(entity =>
        {
            entity.ToTable("characterShape");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).ValueGeneratedNever();
            entity.HasOne(x => x.Character).WithOne(c => c.Shape).HasForeignKey<CharacterShape>(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterStatPoints>(entity =>
        {
            entity.ToTable("characterStatPoints");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).ValueGeneratedNever();
            entity.HasOne(x => x.Character).WithOne(c => c.StatPoints).HasForeignKey<CharacterStatPoints>(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterTutorial>(entity =>
        {
            entity.ToTable("characterTutorial");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).ValueGeneratedNever();
            entity.HasOne(x => x.Character).WithOne(c => c.Tutorial).HasForeignKey<CharacterTutorial>(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterOptions>(entity =>
        {
            entity.ToTable("characterOptions");
            entity.HasKey(x => x.CharacterId);
            entity.Property(x => x.CharacterId).ValueGeneratedNever();
            entity.HasOne(x => x.Character).WithOne(c => c.Options).HasForeignKey<CharacterOptions>(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CharacterSkill>(entity =>
        {
            entity.ToTable("characterSkill");
            entity.HasKey(x => new { x.CharacterId, x.SkillId });
            entity.HasOne(x => x.Character).WithMany(c => c.Skills).HasForeignKey(x => x.CharacterId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
