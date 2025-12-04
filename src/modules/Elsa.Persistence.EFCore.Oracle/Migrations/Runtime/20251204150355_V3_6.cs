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
            migrationBuilder.AlterColumn<string>(
                name: "SerializedInput",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedBookmarkPayload",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityState",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "Triggers",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedMetadata",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedProperties",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedOutputs",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedException",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityState",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NVARCHAR2(2000)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NCLOB",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                columns: new[] { "WorkflowDefinitionId", "Hash", "ActivityId" },
                unique: true,
                filter: "\"Hash\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId",
                schema: _schema.Schema,
                table: "Triggers");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedInput",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedBookmarkPayload",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityState",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "Triggers",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedMetadata",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedProperties",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedOutputs",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedException",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityState",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "NCLOB",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)",
                oldNullable: true);
        }
    }
}
