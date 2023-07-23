using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
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
                    ActivityType = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityTypeVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityName = table.Column<string>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<string>(type: "TEXT", nullable: false),
                    HasBookmarks = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityExecutionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    BookmarkId = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityTypeName = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.BookmarkId);
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
                    NodeId = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<string>(type: "TEXT", nullable: false),
                    Sequence = table.Column<long>(type: "INTEGER", nullable: false),
                    EventName = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    ActivityData = table.Column<string>(type: "TEXT", nullable: true),
                    PayloadData = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowExecutionLogRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowStates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionVersionId = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    SubStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutionLogSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<string>(type: "TEXT", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTriggers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ActivityId = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTriggers", x => x.Id);
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
                name: "IX_ActivityExecutionRecord_WorkflowInstanceId",
                table: "ActivityExecutionRecords",
                column: "WorkflowInstanceId");

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
                name: "IX_StoredBookmark_Hash",
                table: "Bookmarks",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_WorkflowInstanceId",
                table: "Bookmarks",
                column: "WorkflowInstanceId");

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

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_CorrelationId",
                table: "WorkflowStates",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_CreatedAt",
                table: "WorkflowStates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_DefinitionId",
                table: "WorkflowStates",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_DefinitionVersionId",
                table: "WorkflowStates",
                column: "DefinitionVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_Status_DefinitionId",
                table: "WorkflowStates",
                columns: new[] { "Status", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_Status_SubStatus",
                table: "WorkflowStates",
                columns: new[] { "Status", "SubStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_Status_SubStatus_DefinitionId_DefinitionVersion",
                table: "WorkflowStates",
                columns: new[] { "Status", "SubStatus", "DefinitionId", "DefinitionVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowState_UpdatedAt",
                table: "WorkflowStates",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Hash",
                table: "WorkflowTriggers",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Name",
                table: "WorkflowTriggers",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_WorkflowDefinitionId",
                table: "WorkflowTriggers",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_WorkflowDefinitionVersionId",
                table: "WorkflowTriggers",
                column: "WorkflowDefinitionVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityExecutionRecords");

            migrationBuilder.DropTable(
                name: "Bookmarks");

            migrationBuilder.DropTable(
                name: "WorkflowExecutionLogRecords");

            migrationBuilder.DropTable(
                name: "WorkflowStates");

            migrationBuilder.DropTable(
                name: "WorkflowTriggers");
        }
    }
}
