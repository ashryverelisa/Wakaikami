using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wakaikami.Database.Migrations.World.Migrations;

/// <inheritdoc />
public partial class AddCharacterSoftDeleteAndSkills : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_character_AccountId_Slot", table: "character");

        migrationBuilder.AddColumn<DateTime>(name: "DeletedAt", table: "character", type: "datetime2", nullable: true);

        migrationBuilder.AddColumn<bool>(name: "IsDeleted", table: "character", type: "bit", nullable: false, defaultValue: false);

        migrationBuilder.CreateTable(
            name: "characterSkill",
            columns: table => new
            {
                CharacterId = table.Column<int>(type: "int", nullable: false),
                SkillId = table.Column<int>(type: "int", nullable: false),
                CoolTime = table.Column<int>(type: "int", nullable: false),
                EmpowerDamage = table.Column<byte>(type: "tinyint", nullable: false),
                EmpowerSp = table.Column<byte>(type: "tinyint", nullable: false),
                EmpowerKeepTime = table.Column<byte>(type: "tinyint", nullable: false),
                EmpowerCoolTime = table.Column<byte>(type: "tinyint", nullable: false),
                Mastery = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_characterSkill", x => new { x.CharacterId, x.SkillId });
                table.ForeignKey(
                    name: "FK_characterSkill_character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "character",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_character_AccountId_Slot",
            table: "character",
            columns: new[] { "AccountId", "Slot" },
            unique: true,
            filter: "[IsDeleted] = 0"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "characterSkill");

        migrationBuilder.DropIndex(name: "IX_character_AccountId_Slot", table: "character");

        migrationBuilder.DropColumn(name: "DeletedAt", table: "character");

        migrationBuilder.DropColumn(name: "IsDeleted", table: "character");

        migrationBuilder.CreateIndex(name: "IX_character_AccountId_Slot", table: "character", columns: new[] { "AccountId", "Slot" }, unique: true);
    }
}
