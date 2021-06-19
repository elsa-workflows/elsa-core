using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Webhooks.Persistence.EntityFramework.MySql.Migrations
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
                name: "WebhookDefinitions",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TenantId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Path = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadTypeName = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDefinitions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
