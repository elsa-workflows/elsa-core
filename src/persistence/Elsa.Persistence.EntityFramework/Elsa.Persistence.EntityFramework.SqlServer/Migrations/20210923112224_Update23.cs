using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.SqlServer.Migrations
{
    public partial class Update23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutputStorageProviderName",
                schema: "Elsa",
                table: "WorkflowDefinitions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OutputStorageProviderName",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
