using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CordBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class uploadsuccessful : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "uploaded_successfully",
                table: "coordinates",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "uploaded_successfully",
                table: "coordinates");
        }
    }
}
