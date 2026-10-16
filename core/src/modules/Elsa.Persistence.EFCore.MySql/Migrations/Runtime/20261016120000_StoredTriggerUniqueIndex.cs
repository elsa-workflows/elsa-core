using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.MySql.Migrations.Runtime
{
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Aligns the <c>Triggers</c> unique index with the MySQL model (three columns, no <c>TenantId</c>).
    /// </para>
    /// <para>
    /// <c>V3_6</c> originally created that three-column index. A later hand-edit added <c>TenantId</c> to
    /// <c>Up()</c> without updating the designer or snapshot. Four <c>varchar(255)</c> utf8mb4 columns are
    /// 4,080 bytes, above InnoDB's 3,072-byte limit, so fresh utf8mb4 databases never applied that
    /// <c>Up()</c>. Databases that did apply it (or that created the four-column index by hand) still have
    /// it; this repair drops that index if present and creates the three-column index if it is missing.
    /// </para>
    /// <para>
    /// <c>Down()</c> is a no-op: restoring the four-column index would fail on utf8mb4, and dropping the
    /// three-column index would undo <c>V3_6</c> on databases that never had the four-column form.
    /// </para>
    /// </remarks>
    public partial class StoredTriggerUniqueIndex : Migration
    {
        private const string ThreeColumnIndexName = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId";
        // MySQL identifiers are limited to 64 characters; the four-column name truncates to this.
        private const string FourColumnIndexNameTruncated = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_Ten";
        private const string FourColumnIndexName = "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId";

        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public StoredTriggerUniqueIndex(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropIndexIfPresent(migrationBuilder, FourColumnIndexName);
            DropIndexIfPresent(migrationBuilder, FourColumnIndexNameTruncated);
            CreateUniqueIndexIfMissing(
                migrationBuilder,
                ThreeColumnIndexName,
                "`WorkflowDefinitionId`, `Hash`, `ActivityId`");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }

        private void DropIndexIfPresent(MigrationBuilder migrationBuilder, string indexName)
        {
            var schemaClause = TableSchemaClause();
            migrationBuilder.Sql($"""
                SET @exist := (SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema {schemaClause} AND table_name = 'Triggers'
                    AND index_name = '{indexName}');
                SET @sqlstmt := IF(@exist > 0, 'DROP INDEX `{indexName}` ON `Triggers`', 'SELECT 1');
                PREPARE stmt FROM @sqlstmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        private void CreateUniqueIndexIfMissing(MigrationBuilder migrationBuilder, string indexName, string columns)
        {
            var schemaClause = TableSchemaClause();
            migrationBuilder.Sql($"""
                SET @exist := (SELECT COUNT(*) FROM information_schema.statistics
                    WHERE table_schema {schemaClause} AND table_name = 'Triggers'
                    AND index_name = '{indexName}');
                SET @sqlstmt := IF(@exist = 0, 'CREATE UNIQUE INDEX `{indexName}` ON `Triggers` ({columns})', 'SELECT 1');
                PREPARE stmt FROM @sqlstmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        // Pomelo is configured with SchemaBehavior.Ignore, so tables live in DATABASE(). Also match the
        // configured Elsa schema name in case a host does qualify objects that way.
        private string TableSchemaClause()
        {
            if (_schema.Schema == null)
                return "= DATABASE()";

            var escaped = _schema.Schema.Replace("'", "''");
            return $"IN (DATABASE(), '{escaped}')";
        }
    }
}
