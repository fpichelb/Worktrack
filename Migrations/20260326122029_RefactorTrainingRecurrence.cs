using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worktrack.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTrainingRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurrenceInterval",
                table: "TrainingEvents");

            migrationBuilder.DropColumn(
                name: "RecurrenceKey",
                table: "TrainingEvents");

            migrationBuilder.DropColumn(
                name: "RecurrencePattern",
                table: "TrainingEvents");

            migrationBuilder.DropColumn(
                name: "RecurrenceUntil",
                table: "TrainingEvents");

            migrationBuilder.AddColumn<DateOnly>(
                name: "OccurrenceDate",
                table: "TrainingEvents",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrainingSeriesId",
                table: "TrainingEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrainingSeries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrainingRoomId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecurrenceInterval = table.Column<int>(type: "int", nullable: false),
                    UntilDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    RecommendedParticipants = table.Column<int>(type: "int", nullable: false),
                    AllowMemberRegistration = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowGroupRegistration = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AllowParticipantEventSubmission = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSeries_TrainingRooms_TrainingRoomId",
                        column: x => x.TrainingRoomId,
                        principalTable: "TrainingRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingSeriesExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TrainingSeriesId = table.Column<int>(type: "int", nullable: false),
                    OccurrenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCancelled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSeriesExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSeriesExceptions_TrainingSeries_TrainingSeriesId",
                        column: x => x.TrainingSeriesId,
                        principalTable: "TrainingSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEvents_TrainingSeriesId",
                table: "TrainingEvents",
                column: "TrainingSeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSeries_TrainingRoomId",
                table: "TrainingSeries",
                column: "TrainingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSeriesExceptions_TrainingSeriesId_OccurrenceDate",
                table: "TrainingSeriesExceptions",
                columns: new[] { "TrainingSeriesId", "OccurrenceDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingEvents_TrainingSeries_TrainingSeriesId",
                table: "TrainingEvents",
                column: "TrainingSeriesId",
                principalTable: "TrainingSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainingEvents_TrainingSeries_TrainingSeriesId",
                table: "TrainingEvents");

            migrationBuilder.DropTable(
                name: "TrainingSeriesExceptions");

            migrationBuilder.DropTable(
                name: "TrainingSeries");

            migrationBuilder.DropIndex(
                name: "IX_TrainingEvents_TrainingSeriesId",
                table: "TrainingEvents");

            migrationBuilder.DropColumn(
                name: "OccurrenceDate",
                table: "TrainingEvents");

            migrationBuilder.DropColumn(
                name: "TrainingSeriesId",
                table: "TrainingEvents");

            migrationBuilder.AddColumn<int>(
                name: "RecurrenceInterval",
                table: "TrainingEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceKey",
                table: "TrainingEvents",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RecurrencePattern",
                table: "TrainingEvents",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceUntil",
                table: "TrainingEvents",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
