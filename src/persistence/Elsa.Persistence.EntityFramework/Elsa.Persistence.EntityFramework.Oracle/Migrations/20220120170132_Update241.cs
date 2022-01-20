using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.Oracle.Migrations
{
    public partial class Update241 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "NVARCHAR2(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(2000)");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_DefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "DefinitionVersionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_DefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.AlterColumn<string>(
                name: "DefinitionVersionId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "NVARCHAR2(2000)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(450)");
        }
    }
}
