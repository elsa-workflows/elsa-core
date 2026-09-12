using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Runtime
{
    /// <inheritdoc />
    /// <remarks>
    /// Oracle rejects an in-place <c>ALTER TABLE ... MODIFY</c> that changes a column's datatype to or from a LOB type
    /// (ORA-22858 / ORA-22859), so <c>ActivityNodeId</c> is converted between NVARCHAR2(450) and NCLOB with the
    /// add/copy/drop/rename sequence <see cref="MigrationHelper.ConvertColumnType"/> emits. Because Oracle commits DDL
    /// implicitly, a run that fails partway leaves its earlier statements applied; every step here is therefore
    /// guarded so that a re-run picks up where the failed one stopped instead of failing on an already-created index
    /// or an already-added column.
    /// </remarks>
    public partial class V3_6 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_6(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The original CreateIndex call passed filter: "\"Hash\" IS NOT NULL", but the Oracle EF provider never
            // emitted it - Oracle has no partial indexes - so this unfiltered statement reproduces what the provider
            // was actually generating (verified against the pre-fix generated script).
            MigrationHelper.CreateIndexIfMissing(
                migrationBuilder,
                _schema,
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId",
                table: "Triggers",
                columns: new[] { "WorkflowDefinitionId", "Hash", "ActivityId", "TenantId" },
                unique: true);

            // Dropping the column below takes its indexes with it, but they are still dropped up front: that is what the
            // migration originally intended, and it also clears an index left behind on a database where the column was
            // already converted by hand.
            MigrationHelper.DropIndexIfPresent(migrationBuilder, _schema, "IX_WorkflowExecutionLogRecord_ActivityNodeId");
            MigrationHelper.DropIndexIfPresent(migrationBuilder, _schema, "IX_ActivityExecutionRecord_ActivityNodeId");

            ConvertActivityNodeIdToNclob(migrationBuilder, "WorkflowExecutionLogRecords");
            ConvertActivityNodeIdToNclob(migrationBuilder, "ActivityExecutionRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The upgraded NCLOB column permits values longer than the NVARCHAR2(450) the downgrade converts back
            // to. Copying such a value would silently truncate it, so both tables are preflighted for oversized
            // values before any statement of the downgrade runs - including the trigger-index drop below - so that
            // an oversized value in either table is caught before Oracle has committed any DDL.
            MigrationHelper.EnsureLobLengthAtMost(migrationBuilder, _schema, "WorkflowExecutionLogRecords", "ActivityNodeId", 450);
            MigrationHelper.EnsureLobLengthAtMost(migrationBuilder, _schema, "ActivityExecutionRecords", "ActivityNodeId", 450);

            MigrationHelper.DropIndexIfPresent(migrationBuilder, _schema, "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId");

            ConvertActivityNodeIdToNVarchar2(migrationBuilder, "WorkflowExecutionLogRecords");
            ConvertActivityNodeIdToNVarchar2(migrationBuilder, "ActivityExecutionRecords");

            MigrationHelper.CreateIndexIfMissing(migrationBuilder, _schema, "IX_WorkflowExecutionLogRecord_ActivityNodeId", "WorkflowExecutionLogRecords", new[] { "ActivityNodeId" });
            MigrationHelper.CreateIndexIfMissing(migrationBuilder, _schema, "IX_ActivityExecutionRecord_ActivityNodeId", "ActivityExecutionRecords", new[] { "ActivityNodeId" });
        }

        private void ConvertActivityNodeIdToNclob(MigrationBuilder migrationBuilder, string table)
        {
            MigrationHelper.ConvertColumnType(
                migrationBuilder,
                _schema,
                table: table,
                column: "ActivityNodeId",
                fromDataType: "NVARCHAR2",
                toDataType: "NCLOB",
                toColumnDefinition: "NCLOB",
                copyExpression: source => $"TO_NCLOB({source})",
                notNull: true);
        }

        private void ConvertActivityNodeIdToNVarchar2(MigrationBuilder migrationBuilder, string table)
        {
            MigrationHelper.ConvertColumnType(
                migrationBuilder,
                _schema,
                table: table,
                column: "ActivityNodeId",
                fromDataType: "NCLOB",
                toDataType: "NVARCHAR2",
                toColumnDefinition: "NVARCHAR2(450)",
                // Activity node IDs are far shorter than 450 characters; DBMS_LOB.SUBSTR counts characters for an
                // NCLOB and returns NVARCHAR2, so it neither truncates real data nor goes through the database
                // character set the way TO_CHAR would.
                copyExpression: source => $"DBMS_LOB.SUBSTR({source}, 450, 1)",
                notNull: true);
        }
    }
}
