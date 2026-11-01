using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Management
{
    /// <inheritdoc />
    public partial class TenantScopedDefinitionIds : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public TenantScopedDefinitionIds(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Oracle stores the default tenant ('') as NULL and treats rows with equal non-null columns and NULL
            // TenantId as duplicates in a unique index, so no stamping is needed here.
            migrationBuilder.DropIndex(
                name: "IX_WorkflowDefinition_DefinitionId_Version",
                schema: _schema.Schema,
                table: "WorkflowDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_DefinitionId_TenantId_Version",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                columns: new[] { "DefinitionId", "TenantId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Oracle commits DROP INDEX. If two tenants share a (DefinitionId, Version), recreating the old unique
            // index would fail after the new one is gone, so check first and keep the migration applied.
            migrationBuilder.Sql($"""
                DECLARE
                    duplicate_count NUMBER;
                BEGIN
                    SELECT COUNT(*)
                    INTO duplicate_count
                    FROM (
                        SELECT "DefinitionId", "Version"
                        FROM "{_schema.Schema}"."WorkflowDefinitions"
                        GROUP BY "DefinitionId", "Version"
                        HAVING COUNT(*) > 1
                    );

                    IF duplicate_count > 0 THEN
                        RAISE_APPLICATION_ERROR(
                            -20007,
                            duplicate_count || ' (DefinitionId, Version) pair(s) exist in more than one tenant, so IX_WorkflowDefinition_DefinitionId_Version cannot be recreated. Remove or re-key the duplicate definitions before downgrading.'
                        );
                    END IF;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_WorkflowDefinition_DefinitionId_TenantId_Version",
                schema: _schema.Schema,
                table: "WorkflowDefinitions");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_DefinitionId_Version",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                columns: new[] { "DefinitionId", "Version" },
                unique: true);
        }
    }
}
