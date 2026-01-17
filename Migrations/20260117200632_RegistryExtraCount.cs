using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worktrack.Migrations
{
    /// <inheritdoc />
    public partial class RegistryExtraCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Extra",
                table: "ActivityRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Extra",
                table: "ActivityRegistrations");
        }
    }
}
