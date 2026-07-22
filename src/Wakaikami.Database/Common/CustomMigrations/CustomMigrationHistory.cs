namespace Wakaikami.Database.Common.CustomMigrations;

public class CustomMigrationHistory
{
    public int Id { get; set; }
    public required string MigrationName { get; set; }
    public DateTime AppliedAt { get; set; }
}
