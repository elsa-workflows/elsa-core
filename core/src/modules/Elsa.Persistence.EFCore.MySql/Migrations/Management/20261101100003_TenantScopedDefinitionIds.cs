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
            // MySQL commits DDL implicitly. If two tenants share a (DefinitionId, Version), recreating the old unique
            // index would fail after the new one is gone, so abort first. SIGNAL MESSAGE_TEXT is limited to 128 characters.
            migrationBuilder.Sql("""
                SET @duplicate_keys := (
                    SELECT GROUP_CONCAT(CONCAT('(', `DefinitionId`, ', ', `Version`, ')') SEPARATOR '; ')
                    FROM (
                        SELECT `DefinitionId`, `Version`
                        FROM `WorkflowDefinitions`
                        GROUP BY `DefinitionId`, `Version`
                        HAVING COUNT(*) > 1
                    ) AS Duplicates
                );

                SET @sql := IF(
                    @duplicate_keys IS NOT NULL,
                    CONCAT(
                        'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ''',
                        LEFT(REPLACE(CONCAT(
                            'Duplicate keys. Resolve before downgrade: ',
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
