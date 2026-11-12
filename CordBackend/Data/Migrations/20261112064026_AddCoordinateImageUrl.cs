using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CordBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoordinateImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "image_url",
                table: "coordinates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "image_url",
                table: "coordinates");
        }
    }
}
