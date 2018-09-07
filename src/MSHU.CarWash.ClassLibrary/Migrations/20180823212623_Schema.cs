using Microsoft.EntityFrameworkCore.Migrations;

namespace MSHU.CarWash.ClassLibrary.Migrations
{
    public partial class Schema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateTo",
                table: "Reservation",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "DateFrom",
                table: "Reservation",
                newName: "StartDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Reservation",
                newName: "DateFrom");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Reservation",
                newName: "DateTo");
        }
    }
}
