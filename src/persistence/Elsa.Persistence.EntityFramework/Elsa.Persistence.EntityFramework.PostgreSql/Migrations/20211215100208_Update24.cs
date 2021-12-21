using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.PostgreSql.Migrations
{
    public partial class Update24 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CorrelationId",
                schema: "Elsa",
                table: "Bookmarks",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
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
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
