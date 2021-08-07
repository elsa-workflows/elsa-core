using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Webhooks.Persistence.EntityFramework.PostgreSql.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "WebhookDefinitions",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PayloadTypeName = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDefinition_Description",
                schema: "Elsa",
                table: "WebhookDefinitions",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDefinition_IsEnabled",
                schema: "Elsa",
                table: "WebhookDefinitions",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDefinition_Name",
                schema: "Elsa",
                table: "WebhookDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDefinition_Path",
                schema: "Elsa",
                table: "WebhookDefinitions",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDefinition_PayloadTypeName",
                schema: "Elsa",
                table: "WebhookDefinitions",
                column: "PayloadTypeName");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDefinition_TenantId",
                schema: "Elsa",
                table: "WebhookDefinitions",
                column: "TenantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WebhookDefinitions",
                schema: "Elsa");
        }
    }
}
