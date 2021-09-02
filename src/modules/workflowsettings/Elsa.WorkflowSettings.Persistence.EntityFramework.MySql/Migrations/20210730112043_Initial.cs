using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.WorkflowSettings.Persistence.EntityFramework.MySql.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "WorkflowSettings",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WorkflowBlueprintId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Key = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
