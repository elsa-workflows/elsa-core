using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.MySql.Migrations
{
    public partial class Update23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutputStorageProviderName",
                table: "WorkflowDefinitions");

            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.RenameTable(
                name: "WorkflowInstances",
                newName: "WorkflowInstances");

            migrationBuilder.RenameTable(
                name: "WorkflowExecutionLogRecords",
                newName: "WorkflowExecutionLogRecords");

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitions",
                newName: "WorkflowDefinitions");

            migrationBuilder.RenameTable(
                name: "Bookmarks",
                newName: "Bookmarks");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WorkflowInstances",
                newName: "WorkflowInstances");

            migrationBuilder.RenameTable(
                name: "WorkflowExecutionLogRecords",
                newName: "WorkflowExecutionLogRecords");

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitions",
                newName: "WorkflowDefinitions");

            migrationBuilder.RenameTable(
                name: "Bookmarks",
                newName: "Bookmarks");

            migrationBuilder.AddColumn<string>(
                name: "OutputStorageProviderName",
                table: "WorkflowDefinitions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
