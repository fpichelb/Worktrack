using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Worktrack.Data;

#nullable disable

namespace Worktrack.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260428111500_AddUserAchievementHistory")]
    public partial class AddUserAchievementHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS `UserAchievementHistories` (
                  `Id` int NOT NULL AUTO_INCREMENT,
                  `UserId` int NOT NULL,
                  `Kind` varchar(80) NOT NULL,
                  `Label` varchar(120) NOT NULL,
                  `BadgeText` varchar(32) NOT NULL,
                  `ColorCss` varchar(120) NOT NULL,
                  `ArchiveYear` int NULL,
                  `IsPermanent` tinyint(1) NOT NULL,
                  `AwardedAtUtc` datetime(6) NOT NULL,
                  PRIMARY KEY (`Id`),
                  CONSTRAINT `FK_UserAchievementHistories_Users_UserId`
                    FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
                ) CHARACTER SET utf8mb4;
                """);

            migrationBuilder.Sql("""
                SET @idx_exists := (
                    SELECT COUNT(1)
                    FROM information_schema.statistics
                    WHERE table_schema = DATABASE()
                      AND table_name = 'UserAchievementHistories'
                      AND index_name = 'IX_UserAchievementHistories_UserId_Kind_ArchiveYear_IsPermanent'
                );
                SET @idx_sql := IF(
                    @idx_exists = 0,
                    'CREATE INDEX `IX_UserAchievementHistories_UserId_Kind_ArchiveYear_IsPermanent` ON `UserAchievementHistories` (`UserId`, `Kind`, `ArchiveYear`, `IsPermanent`);',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @idx_sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS `UserAchievementHistories`;
                """);
        }
    }
}
