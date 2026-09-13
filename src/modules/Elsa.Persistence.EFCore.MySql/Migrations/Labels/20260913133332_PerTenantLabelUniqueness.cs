using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.MySql.Migrations.Labels
{
    /// <inheritdoc />
    public partial class PerTenantLabelUniqueness : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public PerTenantLabelUniqueness(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default tenant is "" (not null). Stamp leftover nulls so the unique index covers them.
            migrationBuilder.Sql($"""
                UPDATE `{_schema.Schema}`.`Labels`
                SET `TenantId` = ''
                WHERE `TenantId` IS NULL;
                """);

            // No silent dedupe. List leftover keys and abort; operators must resolve them before upgrading.
            // SIGNAL MESSAGE_TEXT is limited to 128 characters, so the listed keys are truncated.
            migrationBuilder.Sql($"""
                SET @duplicate_keys := (
                    SELECT GROUP_CONCAT(CONCAT('(', IFNULL(`TenantId`, '<null>'), ', ', `NormalizedName`, ')') SEPARATOR '; ')
                    FROM (
                        SELECT `TenantId`, `NormalizedName`
                        FROM `{_schema.Schema}`.`Labels`
                        GROUP BY `TenantId`, `NormalizedName`
                        HAVING COUNT(*) > 1
                    ) AS Duplicates
                );

                SET @sql := IF(
                    @duplicate_keys IS NOT NULL,
                    CONCAT(
                        'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ''',
                        LEFT(REPLACE(CONCAT(
                            'Duplicate keys. Resolve before upgrade: ',
                            @duplicate_keys
                        ), '''', ''), 128),
                        ''''
                    ),
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            // Name/NormalizedName are not truncated; list over-length Ids and abort before ALTER.
            migrationBuilder.Sql($"""
                SET @over_length_ids := (
                    SELECT GROUP_CONCAT(`Id` SEPARATOR ', ')
                    FROM `{_schema.Schema}`.`Labels`
                    WHERE CHAR_LENGTH(`Name`) > 255 OR CHAR_LENGTH(`NormalizedName`) > 255
                );

                SET @sql := IF(
                    @over_length_ids IS NOT NULL,
                    CONCAT(
                        'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ''',
                        LEFT(REPLACE(CONCAT(
                            'Over-length Name/NormalizedName. Shorten before upgrade. Ids: ',
                            @over_length_ids
                        ), '''', ''), 128),
                        ''''
                    ),
                    'SELECT 1'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Labels",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Label_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Label_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Labels");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "NormalizedName",
                schema: _schema.Schema,
                table: "Labels",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Labels",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
