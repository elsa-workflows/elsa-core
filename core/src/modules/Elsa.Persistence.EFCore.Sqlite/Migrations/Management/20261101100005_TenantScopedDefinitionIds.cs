using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Management
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
            // SQLite ignores schemas; the table is created as "WorkflowDefinitions".
            // Default tenant is "" (not null). SQLite treats NULLs as distinct in a unique index, so stamp leftover
            // nulls to keep (DefinitionId, TenantId, Version) unique for them.
            migrationBuilder.Sql("""
                UPDATE "WorkflowDefinitions"
                SET "TenantId" = ''
                WHERE "TenantId" IS NULL;
                """);

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
