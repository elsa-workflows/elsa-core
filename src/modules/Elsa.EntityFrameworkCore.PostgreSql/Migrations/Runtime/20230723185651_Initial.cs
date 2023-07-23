using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "ActivityExecutionRecords",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "text", nullable: false),
                    ActivityId = table.Column<string>(type: "text", nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    ActivityTypeVersion = table.Column<int>(type: "integer", nullable: false),
                    ActivityName = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HasBookmarks = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivityData = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityExecutionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                schema: "Elsa",
                columns: table => new
                {
                    BookmarkId = table.Column<string>(type: "text", nullable: false),
                    ActivityTypeName = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.BookmarkId);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowExecutionLogRecords",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "text", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "text", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "text", nullable: false),
                    WorkflowVersion = table.Column<int>(type: "integer", nullable: false),
                    ActivityInstanceId = table.Column<string>(type: "text", nullable: false),
                    ParentActivityInstanceId = table.Column<string>(type: "text", nullable: true),
                    ActivityId = table.Column<string>(type: "text", nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    ActivityTypeVersion = table.Column<int>(type: "integer", nullable: false),
                    ActivityName = table.Column<string>(type: "text", nullable: true),
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    EventName = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    ActivityData = table.Column<string>(type: "text", nullable: true),
                    PayloadData = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutionLogRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStates",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DefinitionId = table.Column<string>(type: "text", nullable: false),
                    DefinitionVersionId = table.Column<string>(type: "text", nullable: false),
                    DefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubStatus = table.Column<string>(type: "text", nullable: false),
                    ExecutionLogSequence = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTriggers",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "text", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ActivityId = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<string>(type: "text", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTriggers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityId",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityName",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "ActivityName");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityType",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityType_ActivityTypeVersion",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                columns: new[] { "ActivityType", "ActivityTypeVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_ActivityTypeVersion",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "ActivityTypeVersion");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_CompletedAt",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_HasBookmarks",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "HasBookmarks");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_StartedAt",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_WorkflowInstanceId",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName",
                schema: "Elsa",
                table: "Bookmarks",
                column: "ActivityTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName_Hash",
                schema: "Elsa",
                table: "Bookmarks",
                columns: new[] { "ActivityTypeName", "Hash" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_ActivityTypeName_Hash_WorkflowInstanceId",
                schema: "Elsa",
                table: "Bookmarks",
                columns: new[] { "ActivityTypeName", "Hash", "WorkflowInstanceId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_Hash",
                schema: "Elsa",
                table: "Bookmarks",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_WorkflowInstanceId",
                schema: "Elsa",
                table: "Bookmarks",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityInstanceId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityName",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityType",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityType_ActivityTypeVersion",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                columns: new[] { "ActivityType", "ActivityTypeVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ActivityTypeVersion",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityTypeVersion");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_EventName",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "EventName");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_ParentActivityInstanceId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "ParentActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Sequence",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "Sequence");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Timestamp",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_Timestamp_Sequence",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                columns: new[] { "Timestamp", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowDefinitionId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowDefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowDefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowInstanceId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowVersion",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowVersion");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_CorrelationId",
                schema: "Elsa",
                table: "WorkflowStates",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_CreatedAt",
                schema: "Elsa",
                table: "WorkflowStates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_DefinitionId",
                schema: "Elsa",
                table: "WorkflowStates",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_DefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowStates",
                column: "DefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_Status_DefinitionId",
                schema: "Elsa",
                table: "WorkflowStates",
                columns: new[] { "Status", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_Status_SubStatus",
                schema: "Elsa",
                table: "WorkflowStates",
                columns: new[] { "Status", "SubStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_Status_SubStatus_DefinitionId_DefinitionVersion",
                schema: "Elsa",
                table: "WorkflowStates",
                columns: new[] { "Status", "SubStatus", "DefinitionId", "DefinitionVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_UpdatedAt",
                schema: "Elsa",
                table: "WorkflowStates",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Hash",
                schema: "Elsa",
                table: "WorkflowTriggers",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Name",
                schema: "Elsa",
                table: "WorkflowTriggers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_WorkflowDefinitionId",
                schema: "Elsa",
                table: "WorkflowTriggers",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_WorkflowDefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowTriggers",
                column: "WorkflowDefinitionVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityExecutionRecords",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "Bookmarks",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "WorkflowExecutionLogRecords",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "WorkflowStates",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "WorkflowTriggers",
                schema: "Elsa");
        }
    }
}
