using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Labels
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionLabels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    WorkflowDefinitionVersionId = table.Column<string>(type: "TEXT", nullable: false),
                    LabelId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitionLabels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_LabelId",
                table: "WorkflowDefinitionLabels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_WorkflowDefinitionId",
                table: "WorkflowDefinitionLabels",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_WorkflowDefinitionVersionId",
                table: "WorkflowDefinitionLabels",
                column: "WorkflowDefinitionVersionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitionLabels");
        }
    }
}
