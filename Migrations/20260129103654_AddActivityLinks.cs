using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worktrack.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityGroupLink_Activities_ActivityId",
                table: "ActivityGroupLink");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityGroupLink_ActivityGroup_GroupId",
                table: "ActivityGroupLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityGroupLink",
                table: "ActivityGroupLink");

            migrationBuilder.DropIndex(
                name: "IX_ActivityGroupLink_ActivityId",
                table: "ActivityGroupLink");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityGroup",
                table: "ActivityGroup");

            migrationBuilder.RenameTable(
                name: "ActivityGroupLink",
                newName: "ActivityGroupLinks");

            migrationBuilder.RenameTable(
                name: "ActivityGroup",
                newName: "ActivityGroups");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityGroupLink_GroupId",
                table: "ActivityGroupLinks",
                newName: "IX_ActivityGroupLinks_GroupId");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ActivityGroupLinks",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityGroupLinks",
                table: "ActivityGroupLinks",
                columns: new[] { "ActivityId", "GroupId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityGroups",
                table: "ActivityGroups",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityGroupLinks_Activities_ActivityId",
                table: "ActivityGroupLinks",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityGroupLinks_ActivityGroups_GroupId",
                table: "ActivityGroupLinks",
                column: "GroupId",
                principalTable: "ActivityGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityGroupLinks_Activities_ActivityId",
                table: "ActivityGroupLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityGroupLinks_ActivityGroups_GroupId",
                table: "ActivityGroupLinks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityGroups",
                table: "ActivityGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityGroupLinks",
                table: "ActivityGroupLinks");

            migrationBuilder.RenameTable(
                name: "ActivityGroups",
                newName: "ActivityGroup");

            migrationBuilder.RenameTable(
                name: "ActivityGroupLinks",
                newName: "ActivityGroupLink");

            migrationBuilder.RenameIndex(
                name: "IX_ActivityGroupLinks_GroupId",
                table: "ActivityGroupLink",
                newName: "IX_ActivityGroupLink_GroupId");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ActivityGroupLink",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityGroup",
                table: "ActivityGroup",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityGroupLink",
                table: "ActivityGroupLink",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityGroupLink_ActivityId",
                table: "ActivityGroupLink",
                column: "ActivityId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityGroupLink_Activities_ActivityId",
                table: "ActivityGroupLink",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityGroupLink_ActivityGroup_GroupId",
                table: "ActivityGroupLink",
                column: "GroupId",
                principalTable: "ActivityGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
