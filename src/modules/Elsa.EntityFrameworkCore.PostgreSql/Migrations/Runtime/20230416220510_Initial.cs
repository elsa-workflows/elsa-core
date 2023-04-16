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
                    WorkflowInstanceId = table.Column<string>(type: "text", nullable: false),
                    WorkflowVersion = table.Column<int>(type: "integer", nullable: false),
                    ActivityInstanceId = table.Column<string>(type: "text", nullable: false),
                    ParentActivityInstanceId = table.Column<string>(type: "text", nullable: true),
                    ActivityId = table.Column<string>(type: "text", nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    NodeId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    DefinitionVersion = table.Column<int>(type: "integer", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
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
                name: "IX_WorkflowExecutionLogRecord_ActivityType",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "ActivityType");

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
                name: "IX_WorkflowExecutionLogRecord_Timestamp",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_WorkflowDefinitionId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "WorkflowDefinitionId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
