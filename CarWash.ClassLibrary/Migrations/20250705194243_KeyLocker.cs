using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.ClassLibrary.Migrations
{
    /// <inheritdoc />
    public partial class KeyLocker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeyLockerBoxId",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Company",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Company",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Oid",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KeyLockerBox",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LockerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BoxSerial = table.Column<int>(type: "int", nullable: false),
                    Building = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Floor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    IsDoorClosed = table.Column<bool>(type: "bit", nullable: false),
                    IsConnected = table.Column<bool>(type: "bit", nullable: false),
                    LastModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyLockerBox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyLockerBoxHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BoxId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LockerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BoxSerial = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    IsDoorClosed = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyLockerBoxHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeyLockerBox_LockerId_BoxSerial",
                table: "KeyLockerBox",
                columns: new[] { "LockerId", "BoxSerial" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeyLockerBoxHistory_LockerId_BoxSerial",
                table: "KeyLockerBoxHistory",
                columns: new[] { "LockerId", "BoxSerial" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyLockerBox");

            migrationBuilder.DropTable(
                name: "KeyLockerBoxHistory");

            migrationBuilder.DropColumn(
                name: "KeyLockerBoxId",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Company");

            migrationBuilder.DropColumn(
                name: "Oid",
                table: "AspNetUsers");
        }
    }
}
