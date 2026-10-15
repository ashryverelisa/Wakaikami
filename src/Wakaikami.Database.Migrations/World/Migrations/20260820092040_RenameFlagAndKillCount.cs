using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wakaikami.Database.Migrations.World.Migrations;

/// <inheritdoc />
public partial class RenameFlagAndKillCount : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "RnFlag",
            table: "character",
            newName: "IsRename");

        migrationBuilder.AddColumn<int>(
            name: "KillCount",
            table: "character",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "KillCount",
            table: "character");

        migrationBuilder.RenameColumn(
            name: "IsRename",
            table: "character",
            newName: "RnFlag");
    }
}
