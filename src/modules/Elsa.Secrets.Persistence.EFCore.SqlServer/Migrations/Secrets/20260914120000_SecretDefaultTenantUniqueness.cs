using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.SqlServer.Migrations.Secrets
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
                UPDATE [{_schema.Schema}].[Secrets]
                SET [TenantId] = N''
                WHERE [TenantId] IS NULL;
                """);

            // No silent dedupe. List leftover keys and abort; operators must resolve them before upgrading.
            migrationBuilder.Sql($"""
                IF EXISTS (
                    SELECT 1
                    FROM [{_schema.Schema}].[Secrets]
                    GROUP BY [TenantId], [NormalizedName]
                    HAVING COUNT(*) > 1
                )
                BEGIN
                    DECLARE @DuplicateKeys nvarchar(max);

                    SELECT @DuplicateKeys = STRING_AGG(
                        CAST(CONCAT(N'(', ISNULL([TenantId], N'<null>'), N', ', [NormalizedName], N')') AS nvarchar(max)),
                        N'; '
                    )
                    FROM (
                        SELECT [TenantId], [NormalizedName]
                        FROM [{_schema.Schema}].[Secrets]
                        GROUP BY [TenantId], [NormalizedName]
                        HAVING COUNT(*) > 1
                    ) AS Duplicates;

                    THROW 50001, CONCAT(
                        N'Cannot create unique index IX_Secret_TenantId_NormalizedName because leftover duplicate (TenantId, NormalizedName) rows exist. Operators must resolve leftover (TenantId, NormalizedName) rows before upgrade. Duplicate keys: ',
                        LEFT(@DuplicateKeys, 1500)
                    ), 1;
                END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
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
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
