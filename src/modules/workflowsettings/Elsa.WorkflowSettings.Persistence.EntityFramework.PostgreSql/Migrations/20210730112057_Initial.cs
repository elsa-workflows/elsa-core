using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.PostgreSql.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "WorkflowSettings",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorkflowBlueprintId = table.Column<string>(type: "text", nullable: true),
                    Key = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSetting_Key",
                schema: "Elsa",
                table: "WorkflowSettings",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSetting_Value",
                schema: "Elsa",
                table: "WorkflowSettings",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSetting_WorkflowBlueprintId",
                schema: "Elsa",
                table: "WorkflowSettings",
                column: "WorkflowBlueprintId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowSettings",
                schema: "Elsa");
        }
    }
}
