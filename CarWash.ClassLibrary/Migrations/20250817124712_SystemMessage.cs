using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.ClassLibrary.Migrations
{
    /// <inheritdoc />
    public partial class SystemMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemMessage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemMessage", x => x.Id);
                });

            migrationBuilder.AddColumn<string>(
                name: "BillingAddress",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BillingName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "KeyLockerBoxId",
                table: "Reservation",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_KeyLockerBoxId",
                table: "Reservation",
                column: "KeyLockerBoxId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_KeyLockerBox_KeyLockerBoxId",
                table: "Reservation",
                column: "KeyLockerBoxId",
                principalTable: "KeyLockerBox",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemMessage");

            migrationBuilder.DropColumn(
                name: "BillingAddress",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BillingName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "KeyLockerBoxId",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.DropIndex(
                name: "IX_Reservation_KeyLockerBoxId",
                table: "Reservation");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_KeyLockerBox_KeyLockerBoxId",
                table: "Reservation");
        }
    }
}
