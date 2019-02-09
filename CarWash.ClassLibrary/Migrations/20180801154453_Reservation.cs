#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CarWash.ClassLibrary.Migrations
{
    public partial class Reservation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Reservation",
                newName: "DateTo");

            migrationBuilder.AddColumn<string>(
                name: "CarwashComment",
                table: "Reservation",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFrom",
                table: "Reservation",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Reservation",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Mpv",
                table: "Reservation",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Private",
                table: "Reservation",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ServicesJson",
                table: "Reservation",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Reservation",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TimeRequirement",
                table: "Reservation",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "AspNetUsers",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCarwashAdmin",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarwashComment",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "DateFrom",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "Mpv",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "Private",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "ServicesJson",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "TimeRequirement",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsCarwashAdmin",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "DateTo",
                table: "Reservation",
                newName: "Date");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "AspNetUsers",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
