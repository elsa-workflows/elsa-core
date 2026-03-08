using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Runtime
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
            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId",
                schema: _schema.Schema,
                table: "Triggers",
                columns: new[] { "WorkflowDefinitionId", "Hash", "ActivityId", "TenantId" },
                unique: true,
                filter: "\"Hash\" IS NOT NULL");

            // ORA-01418: specified index does not exist
            migrationBuilder.Sql(@"
                BEGIN
                    EXECUTE IMMEDIATE 'DROP INDEX ""IX_WorkflowExecutionLogRecord_ActivityNodeId""';
                EXCEPTION
                    WHEN OTHERS THEN
                        IF SQLCODE != -1418 THEN RAISE; END IF;
                END;
            ");

            migrationBuilder.Sql(@"
                BEGIN
                    EXECUTE IMMEDIATE 'DROP INDEX ""IX_ActivityExecutionRecord_ActivityNodeId""';
                EXCEPTION
                    WHEN OTHERS THEN
                        IF SQLCODE != -1418 THEN RAISE; END IF;
                END;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "NCLOB",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NCLOB",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId",
                schema: _schema.Schema,
                table: "Triggers");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NCLOB");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NCLOB");

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
