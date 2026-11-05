using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CordBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoordinateUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "user_id",
                table: "coordinates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_coordinates_user_id",
                table: "coordinates",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_coordinates_users_user_id",
                table: "coordinates",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_coordinates_users_user_id",
                table: "coordinates");

            migrationBuilder.DropIndex(
                name: "IX_coordinates_user_id",
                table: "coordinates");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "coordinates");
        }
    }
}
