using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.Oracle.Migrations.Secrets
{
    /// <inheritdoc />
    public partial class ManagedSecretOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Secret_TenantId_NormalizedName",
                schema: "Elsa",
                table: "Secrets");

            migrationBuilder.AddColumn<string>(
                name: "ManagedGenerationId",
                schema: "Elsa",
                table: "Secrets",
                type: "NVARCHAR2(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagedOwnerId",
                schema: "Elsa",
                table: "Secrets",
                type: "NVARCHAR2(200)",
                maxLength: 200,
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
            throw new NotSupportedException("This migration is irreversible because dropping ownership markers could make lifecycle credential material mutable.");
        }
    }
}
