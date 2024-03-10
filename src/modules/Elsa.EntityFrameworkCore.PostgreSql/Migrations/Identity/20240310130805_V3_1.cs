using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Identity
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "Roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "Applications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId",
                schema: "Elsa",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId",
                schema: "Elsa",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Application_TenantId",
                schema: "Elsa",
                table: "Applications",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_TenantId",
                schema: "Elsa",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Role_TenantId",
                schema: "Elsa",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Application_TenantId",
                schema: "Elsa",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "Applications");
        }
    }
}
