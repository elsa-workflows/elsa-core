using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.Oracle.Migrations.Secrets
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

            // Default tenant is "" (not null). Stamp leftover nulls so the filtered unique
            // index (TenantId IS NOT NULL) covers them. Duplicates are not deleted.
            migrationBuilder.Sql($"""
                UPDATE "{_schema.Schema}"."Secrets"
                SET "TenantId" = ''
                WHERE "TenantId" IS NULL;
                """);

            // No silent dedupe. List leftover keys and abort; operators must resolve them before upgrading.
            migrationBuilder.Sql($"""
                DECLARE
                    duplicate_keys VARCHAR2(4000);
                    duplicate_count NUMBER;
                BEGIN
                    SELECT COUNT(*)
                    INTO duplicate_count
                    FROM (
                        SELECT "TenantId", "NormalizedName"
                        FROM "{_schema.Schema}"."Secrets"
                        GROUP BY "TenantId", "NormalizedName"
                        HAVING COUNT(*) > 1
                    );

                    IF duplicate_count > 0 THEN
                        SELECT LISTAGG('(' || NVL("TenantId", '<null>') || ', ' || "NormalizedName" || ')', '; ' ON OVERFLOW TRUNCATE)
                            WITHIN GROUP (ORDER BY "NormalizedName")
                        INTO duplicate_keys
                        FROM (
                            SELECT "TenantId", "NormalizedName"
                            FROM "{_schema.Schema}"."Secrets"
                            GROUP BY "TenantId", "NormalizedName"
                            HAVING COUNT(*) > 1
                        );

                        RAISE_APPLICATION_ERROR(
                            -20001,
                            SUBSTR(
                                'Cannot create unique index IX_Secret_TenantId_NormalizedName because leftover duplicate (TenantId, NormalizedName) rows exist. Operators must resolve leftover (TenantId, NormalizedName) rows before upgrade. Duplicate keys: ' || duplicate_keys,
                                1,
                                2048
                            )
                        );
                    END IF;
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");
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
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");
        }
    }
}
