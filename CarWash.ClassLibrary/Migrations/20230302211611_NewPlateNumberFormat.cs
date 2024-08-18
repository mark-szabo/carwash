using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.ClassLibrary.Migrations
{
    /// <inheritdoc />
    public partial class NewPlateNumberFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PushSubscription_Id",
                table: "PushSubscription");

            migrationBuilder.AlterColumn<string>(
                name: "VehiclePlateNumber",
                table: "Reservation",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "PushSubscription",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscription_Id",
                table: "PushSubscription",
                column: "Id",
                unique: true,
                filter: "[Id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PushSubscription_Id",
                table: "PushSubscription");

            migrationBuilder.AlterColumn<string>(
                name: "VehiclePlateNumber",
                table: "Reservation",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "PushSubscription",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscription_Id",
                table: "PushSubscription",
                column: "Id",
                unique: true);
        }
    }
}
