#nullable disable

using Elsa.EntityFrameworkCore.Common.Contracts;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowInboxMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KeyValuePairs",
                table: "KeyValuePairs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bookmarks",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "KeyValuePairs");

            migrationBuilder.DropColumn(
                name: "BookmarkId",
                table: "Bookmarks");

            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.RenameTable(
                name: "WorkflowExecutionLogRecords",
                newName: "WorkflowExecutionLogRecords",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "Triggers",
                newName: "Triggers",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "KeyValuePairs",
                newName: "KeyValuePairs",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "Bookmarks",
                newName: "Bookmarks",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "ActivityExecutionRecords",
                newName: "ActivityExecutionRecords",
                newSchema: "Elsa");

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowVersion",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowInstanceId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            // migrationBuilder.AlterColumn<DateTimeOffset>(
            //     name: "Timestamp",
            //     schema: _schema.Schema,
            //     table: "WorkflowExecutionLogRecords",
            //     type: "timestamp with time zone",
            //     nullable: false,
            //     oldClrType: typeof(string),
            //     oldType: "TEXT");

            MigrationHelper.AlterColumnDateTime(migrationBuilder, _schema, "WorkflowExecutionLogRecords", "Timestamp", false);

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityState",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Sequence",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "ParentActivityInstanceId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActivityTypeVersion",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityName",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityInstanceId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionVersionId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Hash",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedValue",
                schema: _schema.Schema,
                table: "KeyValuePairs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "KeyValuePairs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "KeyValuePairs",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowInstanceId",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedMetadata",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Hash",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            // migrationBuilder.AlterColumn<DateTimeOffset>(
            //     name: "CreatedAt",
            //     schema: _schema.Schema,
            //     table: "Bookmarks",
            //     type: "timestamp with time zone",
            //     nullable: false,
            //     oldClrType: typeof(string),
            //     oldType: "TEXT");

            MigrationHelper.AlterColumnDateTime(migrationBuilder, _schema, "Bookmarks", "CreatedAt", false);

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityTypeName",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityInstanceId",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Bookmarks",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            // migrationBuilder.AlterColumn<DateTimeOffset>(
            //     name: "StartedAt",
            //     schema: _schema.Schema,
            //     table: "ActivityExecutionRecords",
            //     type: "timestamp with time zone",
            //     nullable: false,
            //     oldClrType: typeof(string),
            //     oldType: "TEXT");

            MigrationHelper.AlterColumnDateTime(migrationBuilder, _schema, "ActivityExecutionRecords", "StartedAt", false);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedProperties",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedOutputs",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedException",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityStateCompressionAlgorithm",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityState",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            // migrationBuilder.AlterColumn<bool>(
            //     name: "HasBookmarks",
            //     schema: _schema.Schema,
            //     table: "ActivityExecutionRecords",
            //     type: "boolean",
            //     nullable: false,
            //     oldClrType: typeof(int),
            //     oldType: "INTEGER");

            MigrationHelper.AlterColumnBoolean(migrationBuilder, _schema, "ActivityExecutionRecords", "HasBookmarks", false);

            // migrationBuilder.AlterColumn<DateTimeOffset>(
            //     name: "CompletedAt",
            //     schema: _schema.Schema,
            //     table: "ActivityExecutionRecords",
            //     type: "timestamp with time zone",
            //     nullable: true,
            //     oldClrType: typeof(string),
            //     oldType: "TEXT",
            //     oldNullable: true);

            MigrationHelper.AlterColumnDateTime(migrationBuilder, _schema, "ActivityExecutionRecords", "CompletedAt", true);

            migrationBuilder.AlterColumn<int>(
                name: "ActivityTypeVersion",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityName",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_KeyValuePairs",
                schema: _schema.Schema,
                table: "KeyValuePairs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bookmarks",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "BookmarkQueueItems",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "text", nullable: true),
                    BookmarkId = table.Column<string>(type: "text", nullable: true),
                    BookmarkHash = table.Column<string>(type: "text", nullable: true),
                    ActivityInstanceId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SerializedOptions = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookmarkQueueItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_TenantId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_TenantId",
                schema: _schema.Schema,
                table: "Triggers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SerializedKeyValuePair_TenantId",
                schema: _schema.Schema,
                table: "KeyValuePairs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_TenantId",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_TenantId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_BookmarkHash",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "BookmarkHash");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_BookmarkId",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "BookmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_CreatedAt",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_TenantId",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookmarkQueueItems",
                schema: _schema.Schema);

            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutionLogRecord_TenantId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords");

            migrationBuilder.DropIndex(
                name: "IX_StoredTrigger_TenantId",
                schema: _schema.Schema,
                table: "Triggers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_KeyValuePairs",
                schema: _schema.Schema,
                table: "KeyValuePairs");

            migrationBuilder.DropIndex(
                name: "IX_SerializedKeyValuePair_TenantId",
                schema: _schema.Schema,
                table: "KeyValuePairs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bookmarks",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_TenantId",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecord_TenantId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Triggers");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: _schema.Schema,
                table: "KeyValuePairs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "KeyValuePairs");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords");

            migrationBuilder.RenameTable(
                name: "WorkflowExecutionLogRecords",
                schema: _schema.Schema,
                newName: "WorkflowExecutionLogRecords");

            migrationBuilder.RenameTable(
                name: "Triggers",
                schema: _schema.Schema,
                newName: "Triggers");

            migrationBuilder.RenameTable(
                name: "KeyValuePairs",
                schema: _schema.Schema,
                newName: "KeyValuePairs");

            migrationBuilder.RenameTable(
                name: "Bookmarks",
                schema: _schema.Schema,
                newName: "Bookmarks");

            migrationBuilder.RenameTable(
                name: "ActivityExecutionRecords",
                schema: _schema.Schema,
                newName: "ActivityExecutionRecords");

            migrationBuilder.AlterColumn<int>(
                name: "WorkflowVersion",
                table: "WorkflowExecutionLogRecords",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowInstanceId",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionVersionId",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionId",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Timestamp",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityState",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Sequence",
                table: "WorkflowExecutionLogRecords",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "ParentActivityInstanceId",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Message",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventName",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActivityTypeVersion",
                table: "WorkflowExecutionLogRecords",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityName",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityInstanceId",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionVersionId",
                table: "Triggers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowDefinitionId",
                table: "Triggers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                table: "Triggers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Triggers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Hash",
                table: "Triggers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                table: "Triggers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Triggers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedValue",
                table: "KeyValuePairs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "KeyValuePairs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowInstanceId",
                table: "Bookmarks",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                table: "Bookmarks",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedMetadata",
                table: "Bookmarks",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Hash",
                table: "Bookmarks",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedAt",
                table: "Bookmarks",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                table: "Bookmarks",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityTypeName",
                table: "Bookmarks",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityInstanceId",
                table: "Bookmarks",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookmarkId",
                table: "Bookmarks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "WorkflowInstanceId",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "StartedAt",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "SerializedProperties",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedPayload",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedOutputs",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedException",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityStateCompressionAlgorithm",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SerializedActivityState",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HasBookmarks",
                table: "ActivityExecutionRecords",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "CompletedAt",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActivityTypeVersion",
                table: "ActivityExecutionRecords",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityNodeId",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityName",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_KeyValuePairs",
                table: "KeyValuePairs",
                column: "Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bookmarks",
                table: "Bookmarks",
                column: "BookmarkId");

            migrationBuilder.CreateTable(
                name: "WorkflowInboxMessages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityTypeName = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    SerializedBookmarkPayload = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedInput = table.Column<string>(type: "TEXT", nullable: true),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_ActivityInstanceId",
                table: "WorkflowInboxMessages",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_ActivityTypeName",
                table: "WorkflowInboxMessages",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_CorrelationId",
                table: "WorkflowInboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_CreatedAt",
                table: "WorkflowInboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_ExpiresAt",
                table: "WorkflowInboxMessages",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_Hash",
                table: "WorkflowInboxMessages",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_WorkflowInstanceId",
                table: "WorkflowInboxMessages",
                column: "WorkflowInstanceId");
        }
    }
}