using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Worktrack.Data;

#nullable disable

namespace Worktrack.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260411121500_AddTrainingPlannerAllDayAndAllRooms")]
    /// <inheritdoc />
    public partial class AddTrainingPlannerAllDayAndAllRooms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'TrainingEvents'
                      AND column_name = 'AppliesToAllRooms'
                );
                SET @col_sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `TrainingEvents` ADD COLUMN `AppliesToAllRooms` tinyint(1) NOT NULL DEFAULT 0;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'TrainingEvents'
                      AND column_name = 'IsAllDay'
                );
                SET @col_sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `TrainingEvents` ADD COLUMN `IsAllDay` tinyint(1) NOT NULL DEFAULT 0;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'TrainingSeries'
                      AND column_name = 'AppliesToAllRooms'
                );
                SET @col_sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `TrainingSeries` ADD COLUMN `AppliesToAllRooms` tinyint(1) NOT NULL DEFAULT 0;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'TrainingSeries'
                      AND column_name = 'IsAllDay'
                );
                SET @col_sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `TrainingSeries` ADD COLUMN `IsAllDay` tinyint(1) NOT NULL DEFAULT 0;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'TrainingEvents'
                      AND column_name = 'AppliesToAllRooms'
                );
                SET @col_sql := IF(
                    @col_exists = 1,
                    'ALTER TABLE `TrainingEvents` DROP COLUMN `AppliesToAllRooms`;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'TrainingEvents'
                      AND column_name = 'IsAllDay'
                );
                SET @col_sql := IF(
                    @col_exists = 1,
                    'ALTER TABLE `TrainingEvents` DROP COLUMN `IsAllDay`;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'TrainingSeries'
                      AND column_name = 'AppliesToAllRooms'
                );
                SET @col_sql := IF(
                    @col_exists = 1,
                    'ALTER TABLE `TrainingSeries` DROP COLUMN `AppliesToAllRooms`;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'TrainingSeries'
                      AND column_name = 'IsAllDay'
                );
                SET @col_sql := IF(
                    @col_exists = 1,
                    'ALTER TABLE `TrainingSeries` DROP COLUMN `IsAllDay`;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
