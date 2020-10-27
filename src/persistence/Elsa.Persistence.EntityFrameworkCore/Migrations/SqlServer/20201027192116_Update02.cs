using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFrameworkCore.Migrations.SqlServer
{
    public partial class Update02 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scopes",
                table: "WorkflowInstances");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "WorkflowInstances",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "WorkflowInstances");

            migrationBuilder.AddColumn<string>(
                name: "Scopes",
                table: "WorkflowInstances",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
