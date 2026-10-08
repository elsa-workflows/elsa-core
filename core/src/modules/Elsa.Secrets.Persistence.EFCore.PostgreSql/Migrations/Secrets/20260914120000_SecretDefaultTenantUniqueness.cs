using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.PostgreSql.Migrations.Secrets
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
                UPDATE "{_schema.Schema}"."Secrets"
                SET "TenantId" = ''
                WHERE "TenantId" IS NULL;
                """);

            // No silent dedupe. List leftover keys and abort; operators must resolve them before upgrading.
            migrationBuilder.Sql($"""
                DO $$
                DECLARE keys text;
                BEGIN
                    SELECT string_agg(format('(%s, %s)', "TenantId", "NormalizedName"), ', ')
                    INTO keys
                    FROM (
                        SELECT "TenantId", "NormalizedName"
                        FROM "{_schema.Schema}"."Secrets"
                        GROUP BY "TenantId", "NormalizedName"
                        HAVING COUNT(*) > 1
                    ) d;

                    IF keys IS NOT NULL THEN
                        RAISE EXCEPTION 'Cannot apply IX_Secret_TenantId_NormalizedName. Operators must resolve leftover (TenantId, NormalizedName) rows before upgrade. Duplicate keys: %', keys;
                    END IF;
                END $$;
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
