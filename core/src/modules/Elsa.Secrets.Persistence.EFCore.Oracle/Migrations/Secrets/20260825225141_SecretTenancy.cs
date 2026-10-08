using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.Oracle.Migrations.Secrets
{
    /// <inheritdoc />
    public partial class SecretTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Secret_NormalizedName",
                schema: "Elsa",
                table: "Secrets");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "Secrets",
                type: "NVARCHAR2(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: "Elsa",
                table: "Secrets",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true,
                filter: "\"TenantId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: "Elsa",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "Secrets");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_NormalizedName",
                schema: "Elsa",
                table: "Secrets",
                column: "NormalizedName",
                unique: true);
        }
    }
}
