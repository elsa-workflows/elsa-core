using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Labels
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
                UPDATE "{_schema.Schema}"."Labels"
                SET "TenantId" = ''
                WHERE "TenantId" IS NULL;
                """);

            // No silent dedupe. List leftover keys and abort; operators must resolve them before upgrading.
            migrationBuilder.Sql($"""
                SELECT RAISE(ABORT, 'Cannot apply IX_Label_TenantId_NormalizedName. Operators must resolve leftover (TenantId, NormalizedName) rows before upgrade. Duplicate keys: ' ||
                    (SELECT group_concat('(' || IFNULL("TenantId", '') || ', ' || "NormalizedName" || ')', ', ')
                     FROM (
                         SELECT "TenantId", "NormalizedName"
                         FROM "{_schema.Schema}"."Labels"
                         GROUP BY "TenantId", "NormalizedName"
                         HAVING COUNT(*) > 1
                     )))
                WHERE EXISTS (
                    SELECT 1
                    FROM "{_schema.Schema}"."Labels"
                    GROUP BY "TenantId", "NormalizedName"
                    HAVING COUNT(*) > 1
                );
                """);

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
        }
    }
}
