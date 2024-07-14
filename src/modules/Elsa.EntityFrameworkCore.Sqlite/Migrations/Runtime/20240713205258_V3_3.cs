#nullable disable

using Elsa.EntityFrameworkCore.Common.Contracts;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Runtime
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

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "KeyValuePairs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "BookmarkId",
                table: "Bookmarks",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "WorkflowExecutionLogRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Triggers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "KeyValuePairs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Bookmarks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "ActivityExecutionRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BookmarkQueueItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_TenantId",
                table: "WorkflowExecutionLogRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_TenantId",
                table: "Triggers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SerializedKeyValuePair_TenantId",
                table: "KeyValuePairs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_TenantId",
                table: "Bookmarks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_TenantId",
                table: "ActivityExecutionRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_StimulusHash",
                table: "BookmarkQueueItems",
                column: "StimulusHash");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_BookmarkId",
                table: "BookmarkQueueItems",
                column: "BookmarkId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_CreatedAt",
                table: "BookmarkQueueItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_TenantId",
                table: "BookmarkQueueItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_WorkflowInstanceId",
                table: "BookmarkQueueItems",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_ActivityInstanceId",
                table: "BookmarkQueueItems",
                column: "ActivityInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_BookmarkQueueItem_ActivityTypeName",
                table: "BookmarkQueueItems",
                column: "ActivityTypeName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookmarkQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutionLogRecord_TenantId",
                table: "WorkflowExecutionLogRecords");

            migrationBuilder.DropIndex(
                name: "IX_StoredTrigger_TenantId",
                table: "Triggers");

            migrationBuilder.DropIndex(
                name: "IX_SerializedKeyValuePair_TenantId",
                table: "KeyValuePairs");

            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_TenantId",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecord_TenantId",
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowExecutionLogRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Triggers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "KeyValuePairs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ActivityExecutionRecords");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "KeyValuePairs",
                newName: "Key");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Bookmarks",
                newName: "BookmarkId");

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