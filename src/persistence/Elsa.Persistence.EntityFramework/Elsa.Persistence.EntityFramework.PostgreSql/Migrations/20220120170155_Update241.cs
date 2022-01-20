using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.PostgreSql.Migrations
{
    public partial class Update241 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
