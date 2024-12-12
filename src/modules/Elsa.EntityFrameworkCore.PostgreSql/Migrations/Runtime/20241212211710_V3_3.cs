using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Runtime
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
            migrationBuilder.DropPrimaryKey(
                name: "PK_KeyValuePairs",
                schema: _schema.Schema,
                table: "KeyValuePairs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bookmarks",
                schema: _schema.Schema,
                table: "Bookmarks");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowInboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "text",
                nullable: true);

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
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    BookmarkId = table.Column<string>(type: "text", nullable: true),
                    StimulusHash = table.Column<string>(type: "text", nullable: true),
                    ActivityInstanceId = table.Column<string>(type: "text", nullable: true),
                    ActivityTypeName = table.Column<string>(type: "text", nullable: true),
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
                table: "WorkflowInboxMessages");

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

            migrationBuilder.AddPrimaryKey(
                name: "PK_KeyValuePairs",
                schema: _schema.Schema,
                table: "KeyValuePairs",
                column: "Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bookmarks",
                schema: _schema.Schema,
                table: "Bookmarks",
                column: "BookmarkId");
        }
    }
}
