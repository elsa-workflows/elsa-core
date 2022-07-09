using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EntityFramework.PostgreSql.Migrations
{
    public partial class Update25 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Triggers",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    ModelType = table.Column<string>(type: "text", nullable: false),
                    ActivityType = table.Column<string>(type: "text", nullable: false),
                    ActivityId = table.Column<string>(type: "text", nullable: false),
                    WorkflowDefinitionId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Triggers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_ActivityId",
                schema: "Elsa",
                table: "Triggers",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_ActivityType",
                schema: "Elsa",
                table: "Triggers",
                column: "ActivityType");

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_ActivityType_TenantId_Hash",
                schema: "Elsa",
                table: "Triggers",
                columns: new[] { "ActivityType", "TenantId", "Hash" });

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_Hash",
                schema: "Elsa",
                table: "Triggers",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_Hash_TenantId",
                schema: "Elsa",
                table: "Triggers",
                columns: new[] { "Hash", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_TenantId",
                schema: "Elsa",
                table: "Triggers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Trigger_WorkflowDefinitionId",
                schema: "Elsa",
                table: "Triggers",
                column: "WorkflowDefinitionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Triggers",
                schema: "Elsa");
        }
    }
}
