using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.SqlServer.Migrations.Runtime
{
    /// <inheritdoc />
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
            // Drop old index if it exists (before TenantId was added)
            migrationBuilder.Sql($@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId'
                    AND object_id = OBJECT_ID('{_schema.Schema}.Triggers'))
                BEGIN
                    DROP INDEX [IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId]
                    ON [{_schema.Schema}].[Triggers]
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId",
                schema: _schema.Schema,
                table: "Triggers",
                columns: new[] { "WorkflowDefinitionId", "Hash", "ActivityId", "TenantId" },
                unique: true,
                filter: "[Hash] IS NOT NULL");

            migrationBuilder.Sql($@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_WorkflowExecutionLogRecord_ActivityNodeId'
                    AND object_id = OBJECT_ID('{_schema.Schema}.WorkflowExecutionLogRecords'))
                BEGIN
                    DROP INDEX [IX_WorkflowExecutionLogRecord_ActivityNodeId]
                    ON [{_schema.Schema}].[WorkflowExecutionLogRecords]
                END
            ");

            migrationBuilder.Sql($@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ActivityExecutionRecord_ActivityNodeId'
                    AND object_id = OBJECT_ID('{_schema.Schema}.ActivityExecutionRecords'))
                BEGIN
                    DROP INDEX [IX_ActivityExecutionRecord_ActivityNodeId]
                    ON [{_schema.Schema}].[ActivityExecutionRecords]
                END
            ");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId",
                schema: _schema.Schema,
                table: "Triggers");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ActivityNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "ActivityNodeId");
        }
    }
}
