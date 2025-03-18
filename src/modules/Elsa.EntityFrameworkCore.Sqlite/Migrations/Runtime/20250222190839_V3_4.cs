using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Runtime
{
    /// <inheritdoc />
    public partial class V3_4 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_4(Elsa.EntityFrameworkCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "WorkflowInboxMessages",
                newName: "WorkflowInboxMessages",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "WorkflowExecutionLogRecords",
                newName: "WorkflowExecutionLogRecords",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "Triggers",
                newName: "Triggers",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "KeyValuePairs",
                newName: "KeyValuePairs",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "Bookmarks",
                newName: "Bookmarks",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "BookmarkQueueItems",
                newName: "BookmarkQueueItems",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "ActivityExecutionRecords",
                newName: "ActivityExecutionRecords",
                newSchema: _schema.Schema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WorkflowInboxMessages",
                schema: _schema.Schema,
                newName: "WorkflowInboxMessages");

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
                name: "BookmarkQueueItems",
                schema: _schema.Schema,
                newName: "BookmarkQueueItems");

            migrationBuilder.RenameTable(
                name: "ActivityExecutionRecords",
                schema: _schema.Schema,
                newName: "ActivityExecutionRecords");
        }
    }
}
