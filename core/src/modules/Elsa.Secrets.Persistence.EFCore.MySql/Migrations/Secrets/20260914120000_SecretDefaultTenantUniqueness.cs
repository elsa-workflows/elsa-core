using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.MySql.Migrations.Secrets
{
    /// <inheritdoc />
    public partial class SecretDefaultTenantUniqueness : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public SecretDefaultTenantUniqueness(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets");

            // Default tenant is "" (not null). Stamp leftover nulls so the unique index covers them.
            migrationBuilder.Sql($"""
                UPDATE `{_schema.Schema}`.`Secrets`
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
                        FROM `{_schema.Schema}`.`Secrets`
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

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);
        }
    }
}
