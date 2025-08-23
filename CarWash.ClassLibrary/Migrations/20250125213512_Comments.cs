using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarWash.ClassLibrary.Migrations
{
    /// <inheritdoc />
    public partial class Comments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentsJson",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
                    UPDATE Reservation
                    SET CommentsJson = 
                        CASE 
                            WHEN Comment IS NOT NULL AND CarwashComment IS NOT NULL THEN 
                                '[{""role"":""user"", ""message"":""'+ REPLACE(CAST(Comment AS NVARCHAR(MAX)), CHAR(10), ' ') + '"", ""timestamp"":""'+ FORMAT(CreatedOn, 'yyyy-MM-ddTHH:mm:ss') + '""}, {""role"":""carwash"", ""message"":""'+ REPLACE(CAST(CarwashComment AS NVARCHAR(MAX)), CHAR(10), ' ') + '"", ""timestamp"":""'+ FORMAT(CreatedOn, 'yyyy-MM-ddTHH:mm:ss') + '""}]'
                            WHEN Comment IS NOT NULL THEN
                                '[{""role"":""user"", ""message"":""'+ REPLACE(CAST(Comment AS NVARCHAR(MAX)), CHAR(10), ' ') + '"", ""timestamp"":""'+ FORMAT(CreatedOn, 'yyyy-MM-ddTHH:mm:ss') + '""}]'
                            WHEN CarwashComment IS NOT NULL THEN
                                '[{""role"":""carwash"", ""message"":""'+ CarwashComment + '"", ""timestamp"":""'+ FORMAT(CreatedOn, 'yyyy-MM-ddTHH:mm:ss') + '""}]'
                            ELSE NULL
                        END
                ");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "CarwashComment",
                table: "Reservation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CarwashComment",
                table: "Reservation",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
                    UPDATE Reservation
                    SET 
                        Comment = JSON_VALUE(CommentsJson, '$[0].message'),
                        CarwashComment = JSON_VALUE(CommentsJson, '$[1].message')
                    WHERE CommentsJson IS NOT NULL
                ");

            migrationBuilder.DropColumn(
                name: "CommentsJson",
                table: "Reservation");
        }
    }
}
