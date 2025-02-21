using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EntityFramework.MySql.Migrations
{
    /// <inheritdoc />
    public partial class Update215 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WorkflowInstances",
                schema: "Elsa",
                newName: "WorkflowInstances");

            migrationBuilder.RenameTable(
                name: "WorkflowExecutionLogRecords",
                schema: "Elsa",
                newName: "WorkflowExecutionLogRecords");

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitions",
                schema: "Elsa",
                newName: "WorkflowDefinitions");

            migrationBuilder.RenameTable(
                name: "Triggers",
                schema: "Elsa",
                newName: "Triggers");

            migrationBuilder.RenameTable(
                name: "Bookmarks",
                schema: "Elsa",
                newName: "Bookmarks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.RenameTable(
                name: "WorkflowInstances",
                newName: "WorkflowInstances",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "WorkflowExecutionLogRecords",
                newName: "WorkflowExecutionLogRecords",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitions",
                newName: "WorkflowDefinitions",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "Triggers",
                newName: "Triggers",
                newSchema: "Elsa");

            migrationBuilder.RenameTable(
                name: "Bookmarks",
                newName: "Bookmarks",
                newSchema: "Elsa");
        }
    }
}
