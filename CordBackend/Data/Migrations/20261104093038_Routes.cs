using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CordBackend.Data.Migrations
{
    /// <inheritdoc />
    public partial class Routes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "route_id",
                table: "coordinates",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "routes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.id);
                    table.ForeignKey(
                        name: "FK_routes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_coordinates_route_id",
                table: "coordinates",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "IX_routes_user_id",
                table: "routes",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_coordinates_routes_route_id",
                table: "coordinates",
                column: "route_id",
                principalTable: "routes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_coordinates_routes_route_id",
                table: "coordinates");

            migrationBuilder.DropTable(
                name: "routes");

            migrationBuilder.DropIndex(
                name: "IX_coordinates_route_id",
                table: "coordinates");

            migrationBuilder.DropColumn(
                name: "route_id",
                table: "coordinates");
        }
    }
}
