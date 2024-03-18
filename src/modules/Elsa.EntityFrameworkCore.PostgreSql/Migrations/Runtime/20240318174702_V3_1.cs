using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Bookmarks",
                schema: "Elsa",
                table: "Bookmarks");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "WorkflowInboxMessages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "Triggers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                schema: "Elsa",
                table: "Bookmarks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "Bookmarks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerializedActivityStateCompressionAlgorithm",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerializedProperties",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bookmarks",
                schema: "Elsa",
                table: "Bookmarks",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "KeyValuePairs",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    SerializedValue = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyValuePairs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInboxMessage_TenantId",
                schema: "Elsa",
                table: "WorkflowInboxMessages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionLogRecord_TenantId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_TenantId",
                schema: "Elsa",
                table: "Triggers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredBookmark_TenantId",
                schema: "Elsa",
                table: "Bookmarks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityExecutionRecord_TenantId",
                schema: "Elsa",
                table: "ActivityExecutionRecords",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SerializedKeyValuePair_Key",
                schema: "Elsa",
                table: "KeyValuePairs",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_SerializedKeyValuePair_TenantId",
                schema: "Elsa",
                table: "KeyValuePairs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyValuePairs",
                schema: "Elsa");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowInboxMessage_TenantId",
                schema: "Elsa",
                table: "WorkflowInboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutionLogRecord_TenantId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords");

            migrationBuilder.DropIndex(
                name: "IX_StoredTrigger_TenantId",
                schema: "Elsa",
                table: "Triggers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bookmarks",
                schema: "Elsa",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_StoredBookmark_TenantId",
                schema: "Elsa",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ActivityExecutionRecord_TenantId",
                schema: "Elsa",
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "WorkflowInboxMessages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "WorkflowExecutionLogRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "Triggers");

            migrationBuilder.DropColumn(
                name: "Id",
                schema: "Elsa",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "SerializedActivityStateCompressionAlgorithm",
                schema: "Elsa",
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "SerializedProperties",
                schema: "Elsa",
                table: "ActivityExecutionRecords");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "ActivityExecutionRecords");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bookmarks",
                schema: "Elsa",
                table: "Bookmarks",
                column: "BookmarkId");
        }
    }
}
