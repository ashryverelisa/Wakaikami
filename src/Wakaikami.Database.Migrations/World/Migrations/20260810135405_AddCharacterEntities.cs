using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wakaikami.Database.Migrations.World.Migrations;

/// <inheritdoc />
public partial class AddCharacterEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "character",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                AccountId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Slot = table.Column<byte>(type: "tinyint", nullable: false),
                Level = table.Column<byte>(type: "tinyint", nullable: false),
                Money = table.Column<long>(type: "bigint", nullable: false),
                Hp = table.Column<int>(type: "int", nullable: false),
                Sp = table.Column<int>(type: "int", nullable: false),
                Lp = table.Column<int>(type: "int", nullable: false),
                HpStones = table.Column<int>(type: "int", nullable: false),
                SpStones = table.Column<int>(type: "int", nullable: false),
                Exp = table.Column<long>(type: "bigint", nullable: false),
                Fame = table.Column<int>(type: "int", nullable: false),
                PosX = table.Column<int>(type: "int", nullable: false),
                PosY = table.Column<int>(type: "int", nullable: false),
                Direction = table.Column<byte>(type: "tinyint", nullable: false),
                MapIndex = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                KqHandle = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                KqDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                LoginCount = table.Column<int>(type: "int", nullable: false),
                RnFlag = table.Column<byte>(type: "tinyint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true),
                FriendPoints = table.Column<int>(type: "int", nullable: false),
                LastFriendPoint = table.Column<int>(type: "int", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_character", x => x.Id);
            }
        );

        migrationBuilder.CreateTable(
            name: "customMigrationHistory",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                MigrationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_customMigrationHistory", x => x.Id);
            }
        );

        migrationBuilder.CreateTable(
            name: "characterOptions",
            columns: table => new
            {
                CharacterId = table.Column<int>(type: "int", nullable: false),
                SShortCutData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                SShortCutSize = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                SVideo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                SSound = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                SGame = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                SWindowsPos = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                SKeyMapping = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_characterOptions", x => x.CharacterId);
                table.ForeignKey(
                    name: "FK_characterOptions_character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "character",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "characterShape",
            columns: table => new
            {
                CharacterId = table.Column<int>(type: "int", nullable: false),
                Class = table.Column<byte>(type: "tinyint", nullable: false),
                Race = table.Column<byte>(type: "tinyint", nullable: false),
                IsMale = table.Column<byte>(type: "tinyint", nullable: false),
                Hair = table.Column<byte>(type: "tinyint", nullable: false),
                HairColor = table.Column<byte>(type: "tinyint", nullable: false),
                Face = table.Column<byte>(type: "tinyint", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_characterShape", x => x.CharacterId);
                table.ForeignKey(
                    name: "FK_characterShape_character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "character",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "characterStatPoints",
            columns: table => new
            {
                CharacterId = table.Column<int>(type: "int", nullable: false),
                Points = table.Column<byte>(type: "tinyint", nullable: false),
                Spr = table.Column<byte>(type: "tinyint", nullable: false),
                Int = table.Column<byte>(type: "tinyint", nullable: false),
                Dex = table.Column<byte>(type: "tinyint", nullable: false),
                Str = table.Column<byte>(type: "tinyint", nullable: false),
                End = table.Column<byte>(type: "tinyint", nullable: false),
                RedistributePoint = table.Column<byte>(type: "tinyint", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_characterStatPoints", x => x.CharacterId);
                table.ForeignKey(
                    name: "FK_characterStatPoints_character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "character",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "characterTutorial",
            columns: table => new
            {
                CharacterId = table.Column<int>(type: "int", nullable: false),
                State = table.Column<byte>(type: "tinyint", nullable: false),
                Step = table.Column<byte>(type: "tinyint", nullable: false),
                Date = table.Column<DateTime>(type: "datetime2", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_characterTutorial", x => x.CharacterId);
                table.ForeignKey(
                    name: "FK_characterTutorial_character_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "character",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(name: "IX_character_AccountId_Slot", table: "character", columns: new[] { "AccountId", "Slot" }, unique: true);

        migrationBuilder.CreateIndex(name: "IX_character_Name", table: "character", column: "Name", unique: true);

        migrationBuilder.CreateIndex(name: "IX_customMigrationHistory_MigrationName", table: "customMigrationHistory", column: "MigrationName", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "characterOptions");

        migrationBuilder.DropTable(name: "characterShape");

        migrationBuilder.DropTable(name: "characterStatPoints");

        migrationBuilder.DropTable(name: "characterTutorial");

        migrationBuilder.DropTable(name: "customMigrationHistory");

        migrationBuilder.DropTable(name: "character");
    }
}
