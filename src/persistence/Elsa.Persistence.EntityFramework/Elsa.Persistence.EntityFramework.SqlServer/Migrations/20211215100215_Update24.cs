using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.SqlServer.Migrations
{
    public partial class Update24 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: "Elsa",
                table: "Bookmarks",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: "Elsa",
                table: "Bookmarks",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
