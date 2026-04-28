using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Worktrack.Data;

#nullable disable

namespace Worktrack.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428170000_AddEventArchiveLifecycle")]
    public partial class AddEventArchiveLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'Events'
                      AND column_name = 'IsArchived'
                );
                SET @col_sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `Events` ADD COLUMN `IsArchived` tinyint(1) NOT NULL DEFAULT 0;',
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
                      AND table_name = 'Events'
                      AND column_name = 'ArchivedAtUtc'
                );
                SET @col_sql := IF(
                    @col_exists = 0,
                    'ALTER TABLE `Events` ADD COLUMN `ArchivedAtUtc` datetime(6) NULL;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @col_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'Events'
                      AND column_name = 'ArchivedAtUtc'
                );
                SET @col_sql := IF(
                    @col_exists = 1,
                    'ALTER TABLE `Events` DROP COLUMN `ArchivedAtUtc`;',
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
                      AND table_name = 'Events'
                      AND column_name = 'IsArchived'
                );
                SET @col_sql := IF(
                    @col_exists = 1,
                    'ALTER TABLE `Events` DROP COLUMN `IsArchived`;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @col_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
