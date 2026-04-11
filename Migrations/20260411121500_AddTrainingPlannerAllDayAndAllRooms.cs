using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Worktrack.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPlannerAllDayAndAllRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `TrainingEvents`
                ADD COLUMN IF NOT EXISTS `AppliesToAllRooms` tinyint(1) NOT NULL DEFAULT 0;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `TrainingEvents`
                ADD COLUMN IF NOT EXISTS `IsAllDay` tinyint(1) NOT NULL DEFAULT 0;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `TrainingSeries`
                ADD COLUMN IF NOT EXISTS `AppliesToAllRooms` tinyint(1) NOT NULL DEFAULT 0;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `TrainingSeries`
                ADD COLUMN IF NOT EXISTS `IsAllDay` tinyint(1) NOT NULL DEFAULT 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE `TrainingEvents`
                DROP COLUMN IF EXISTS `AppliesToAllRooms`;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `TrainingEvents`
                DROP COLUMN IF EXISTS `IsAllDay`;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `TrainingSeries`
                DROP COLUMN IF EXISTS `AppliesToAllRooms`;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE `TrainingSeries`
                DROP COLUMN IF EXISTS `IsAllDay`;
                """);
        }
    }
}
