using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.PostgreSql.Migrations.Identity
{
    /// <inheritdoc />
    public partial class PerTenantIdentityUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Name",
                schema: "Elsa",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Role_Name",
                schema: "Elsa",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Application_ClientId",
                schema: "Elsa",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Application_Name",
                schema: "Elsa",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_User_TenantId_Name",
                schema: "Elsa",
                table: "Users",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_TenantId_Name",
                schema: "Elsa",
                table: "Roles",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Application_TenantId_ClientId",
                schema: "Elsa",
                table: "Applications",
                columns: new[] { "TenantId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Application_TenantId_Name",
                schema: "Elsa",
                table: "Applications",
                columns: new[] { "TenantId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_TenantId_Name",
                schema: "Elsa",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Role_TenantId_Name",
                schema: "Elsa",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Application_TenantId_ClientId",
                schema: "Elsa",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Application_TenantId_Name",
                schema: "Elsa",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_User_Name",
                schema: "Elsa",
                table: "Users",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_Name",
                schema: "Elsa",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Application_ClientId",
                schema: "Elsa",
                table: "Applications",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Application_Name",
                schema: "Elsa",
                table: "Applications",
                column: "Name",
                unique: true);
        }
    }
}
