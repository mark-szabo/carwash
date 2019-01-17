#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MSHU.CarWash.ClassLibrary.Migrations
{
    public partial class Graph : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Reservation",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateTo",
                table: "Reservation",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.AddColumn<string>(
                name: "OutlookEventId",
                table: "Reservation",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutlookEventId",
                table: "Reservation");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Reservation",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateTo",
                table: "Reservation",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);
        }
    }
}
