using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Runtime
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
            migrationBuilder.CreateTable(
                name: "ActivityExecutionRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityId = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityNodeId = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityType = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityTypeVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityName = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    HasBookmarks = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SerializedActivityState = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedActivityStateCompressionAlgorithm = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedException = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedOutputs = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedPayload = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedProperties = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityExecutionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookmarkQueueItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                    BookmarkId = table.Column<string>(type: "TEXT", nullable: true),
                    StimulusHash = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityTypeName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SerializedOptions = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookmarkQueueItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityTypeName = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SerializedMetadata = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedPayload = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyValuePairs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SerializedValue = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyValuePairs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Triggers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityId = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedPayload = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Triggers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutionLogRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    ParentActivityInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityId = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityType = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityTypeVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityName = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityNodeId = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    EventName = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedActivityState = table.Column<string>(type: "TEXT", nullable: true),
                    SerializedPayload = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutionLogRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityId",
                table: "ActivityExecutionRecords",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityName",
                table: "ActivityExecutionRecords",
                column: "ActivityName");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityNodeId",
                table: "ActivityExecutionRecords",
                column: "ActivityNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityType",
                table: "ActivityExecutionRecords",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityType_ActivityTypeVersion",
                table: "ActivityExecutionRecords",
                columns: new[] { "ActivityType", "ActivityTypeVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityTypeVersion",
                table: "ActivityExecutionRecords",
                column: "ActivityTypeVersion");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_CompletedAt",
                table: "ActivityExecutionRecords",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_HasBookmarks",
                table: "ActivityExecutionRecords",
                column: "HasBookmarks");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_StartedAt",
                table: "ActivityExecutionRecords",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_Status",
                table: "ActivityExecutionRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_TenantId",
                table: "ActivityExecutionRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_WorkflowInstanceId",
                table: "ActivityExecutionRecords",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_ActivityInstanceId",
                table: "BookmarkQueueItems",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_ActivityTypeName",
                table: "BookmarkQueueItems",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_BookmarkId",
                table: "BookmarkQueueItems",
                column: "BookmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_CorrelationId",
                table: "BookmarkQueueItems",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_CreatedAt",
                table: "BookmarkQueueItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_StimulusHash",
                table: "BookmarkQueueItems",
                column: "StimulusHash");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_TenantId",
                table: "BookmarkQueueItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_WorkflowInstanceId",
                table: "BookmarkQueueItems",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityInstanceId",
                table: "Bookmarks",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName",
                table: "Bookmarks",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName_Hash",
                table: "Bookmarks",
                columns: new[] { "ActivityTypeName", "Hash" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName_Hash_WorkflowInstanceId",
                table: "Bookmarks",
                columns: new[] { "ActivityTypeName", "Hash", "WorkflowInstanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_CreatedAt",
                table: "Bookmarks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Hash",
                table: "Bookmarks",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_TenantId",
                table: "Bookmarks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_WorkflowInstanceId",
                table: "Bookmarks",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_SerializedKeyValuePair_TenantId",
                table: "KeyValuePairs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Hash",
                table: "Triggers",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Name",
                table: "Triggers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_TenantId",
                table: "Triggers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_WorkflowDefinitionId",
                table: "Triggers",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_WorkflowDefinitionVersionId",
                table: "Triggers",
                column: "WorkflowDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityId",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityInstanceId",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityName",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityNodeId",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityType",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityType_ActivityTypeVersion",
                table: "WorkflowExecutionLogRecords",
                columns: new[] { "ActivityType", "ActivityTypeVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityTypeVersion",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityTypeVersion");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_EventName",
                table: "WorkflowExecutionLogRecords",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ParentActivityInstanceId",
                table: "WorkflowExecutionLogRecords",
                column: "ParentActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Sequence",
                table: "WorkflowExecutionLogRecords",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_TenantId",
                table: "WorkflowExecutionLogRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Timestamp",
                table: "WorkflowExecutionLogRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Timestamp_Sequence",
                table: "WorkflowExecutionLogRecords",
                columns: new[] { "Timestamp", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowDefinitionId",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowDefinitionVersionId",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowInstanceId",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowVersion",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityExecutionRecords");

            migrationBuilder.DropTable(
                name: "BookmarkQueueItems");

            migrationBuilder.DropTable(
                name: "Bookmarks");

            migrationBuilder.DropTable(
                name: "KeyValuePairs");

            migrationBuilder.DropTable(
                name: "Triggers");

            migrationBuilder.DropTable(
                name: "WorkflowExecutionLogRecords");
        }
    }
}
