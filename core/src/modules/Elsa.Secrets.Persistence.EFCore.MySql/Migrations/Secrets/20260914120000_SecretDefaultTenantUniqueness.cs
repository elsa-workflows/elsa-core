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
            // MySQL commits DDL immediately. If CreateIndex fails on duplicates, the
            // drop is already persisted while EF leaves the migration unapplied, so a
            // retry must not assume the index still exists.
            migrationBuilder.Sql("""
                SET @exist := (SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema = DATABASE() AND table_name = 'Secrets'
                    AND index_name = 'IX_Secret_TenantId_NormalizedName');
                SET @sqlstmt := IF(@exist > 0, 'DROP INDEX `IX_Secret_TenantId_NormalizedName` ON `Secrets`', 'SELECT 1');
                PREPARE stmt FROM @sqlstmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);

            // Pomelo SchemaBehavior.Ignore stores tables in DATABASE(), not an Elsa schema.
            // Default tenant is "" (not null). Stamp leftover nulls so the unique index covers them.
            migrationBuilder.Sql("""
                UPDATE `Secrets`
                SET `TenantId` = ''
                WHERE `TenantId` IS NULL;
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
