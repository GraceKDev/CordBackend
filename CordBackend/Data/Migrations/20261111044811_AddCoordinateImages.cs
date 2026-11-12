using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CordBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoordinateImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_image",
                table: "coordinates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "image_id",
                table: "coordinates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_image",
                table: "coordinates");

            migrationBuilder.DropColumn(
                name: "image_id",
                table: "coordinates");
        }
    }
}
