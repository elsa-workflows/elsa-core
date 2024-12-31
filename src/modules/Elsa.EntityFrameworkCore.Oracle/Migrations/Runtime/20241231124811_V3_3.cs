using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Oracle.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(Elsa.EntityFrameworkCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "ActivityExecutionRecords",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityNodeId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityType = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityTypeVersion = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ActivityName = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    HasBookmarks = table.Column<bool>(type: "BOOLEAN", nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                    SerializedActivityState = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedActivityStateCompressionAlgorithm = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedException = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedOutputs = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedPayload = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedProperties = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityExecutionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookmarkQueueItems",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    BookmarkId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    StimulusHash = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    ActivityInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    ActivityTypeName = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    SerializedOptions = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookmarkQueueItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityTypeName = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Hash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    SerializedMetadata = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedPayload = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyValuePairs",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Key = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SerializedValue = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyValuePairs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Triggers",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Hash = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    SerializedPayload = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Triggers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutionLogRecords",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowVersion = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ActivityInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ParentActivityInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    ActivityId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityType = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityTypeVersion = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    ActivityName = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    ActivityNodeId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    Sequence = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    EventName = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    Message = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Source = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedActivityState = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedPayload = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutionLogRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInboxMessages",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    ActivityTypeName = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Hash = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    CorrelationId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    ActivityInstanceId = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: false),
                    SerializedBookmarkPayload = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    SerializedInput = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    TenantId = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityName",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "ActivityName");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityNodeId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "ActivityNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityType",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityType_ActivityTypeVersion",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                columns: new[] { "ActivityType", "ActivityTypeVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityTypeVersion",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "ActivityTypeVersion");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_CompletedAt",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_HasBookmarks",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "HasBookmarks");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_StartedAt",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_Status",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_TenantId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "ActivityExecutionRecords",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_ActivityInstanceId",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_ActivityTypeName",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_BookmarkId",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "BookmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_CorrelationId",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_CreatedAt",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_StimulusHash",
                schema: _schema.Schema,
                table: "BookmarkQueueItems",
                column: "StimulusHash");

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

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityInstanceId",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName_Hash",
                schema: _schema.Schema,
                table: "Bookmarks",
                columns: new[] { "ActivityTypeName", "Hash" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName_Hash_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "Bookmarks",
                columns: new[] { "ActivityTypeName", "Hash", "WorkflowInstanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_CreatedAt",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Hash",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_TenantId",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_SerializedKeyValuePair_TenantId",
                schema: _schema.Schema,
                table: "KeyValuePairs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Hash",
                schema: _schema.Schema,
                table: "Triggers",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Name",
                schema: _schema.Schema,
                table: "Triggers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_TenantId",
                schema: _schema.Schema,
                table: "Triggers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_WorkflowDefinitionId",
                schema: _schema.Schema,
                table: "Triggers",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_WorkflowDefinitionVersionId",
                schema: _schema.Schema,
                table: "Triggers",
                column: "WorkflowDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityInstanceId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityName",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ActivityName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityNodeId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ActivityNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityType",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityType_ActivityTypeVersion",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                columns: new[] { "ActivityType", "ActivityTypeVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityTypeVersion",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ActivityTypeVersion");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_EventName",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ParentActivityInstanceId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "ParentActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Sequence",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_TenantId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Timestamp",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Timestamp_Sequence",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                columns: new[] { "Timestamp", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowDefinitionId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowDefinitionVersionId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowVersion",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowVersion");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_ActivityInstanceId",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_ActivityTypeName",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_CreatedAt",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_ExpiresAt",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_Hash",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_WorkflowInstanceId",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityExecutionRecords",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "BookmarkQueueItems",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "Bookmarks",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "KeyValuePairs",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "Triggers",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "WorkflowExecutionLogRecords",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "WorkflowInboxMessages",
                schema: _schema.Schema);
        }
    }
}
