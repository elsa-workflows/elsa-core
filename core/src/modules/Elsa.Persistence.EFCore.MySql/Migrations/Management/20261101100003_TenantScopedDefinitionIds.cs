using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.MySql.Migrations.Management
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
            // Pomelo SchemaBehavior.Ignore stores tables in DATABASE(), not an Elsa schema.
            // Default tenant is "" (not null). MySQL treats NULLs as distinct in a unique index, so stamp leftover
            // nulls to keep (DefinitionId, TenantId, Version) unique for them.
            migrationBuilder.Sql("""
                UPDATE `WorkflowDefinitions`
                SET `TenantId` = ''
                WHERE `TenantId` IS NULL;
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
            // MySQL 8 applies this index replacement atomically. If cross-tenant duplicates prevent
            // building the legacy unique index, MySQL rejects the entire ALTER and retains the current index.
            migrationBuilder.Sql("""
                ALTER TABLE `WorkflowDefinitions`
                    ADD UNIQUE INDEX `IX_WorkflowDefinition_DefinitionId_Version` (`DefinitionId`, `Version`),
                    DROP INDEX `IX_WorkflowDefinition_DefinitionId_TenantId_Version`;
                """);
        }
    }
}
