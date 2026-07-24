using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wakaikami.Database.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsOnlineAndLastLoginAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "account");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "account");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "account",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "account",
                type: "datetime2",
                nullable: true);
        }
    }
}
