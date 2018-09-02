using Microsoft.EntityFrameworkCore.Migrations;

namespace MSHU.CarWash.ClassLibrary.Migrations
{
    public partial class PushSubscription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "Private",
                table: "Reservation",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Mpv",
                table: "Reservation",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "PushSubscription",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserId = table.Column<string>(nullable: false),
                    DeviceId = table.Column<string>(nullable: true),
                    Endpoint = table.Column<string>(nullable: false),
                    ExpirationTime = table.Column<double>(nullable: true),
                    P256Dh = table.Column<string>(nullable: false),
                    Auth = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscription_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscription_UserId",
                table: "PushSubscription",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushSubscription");

            migrationBuilder.AlterColumn<bool>(
                name: "Private",
                table: "Reservation",
                nullable: true,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<bool>(
                name: "Mpv",
                table: "Reservation",
                nullable: true,
                oldClrType: typeof(bool));
        }
    }
}
