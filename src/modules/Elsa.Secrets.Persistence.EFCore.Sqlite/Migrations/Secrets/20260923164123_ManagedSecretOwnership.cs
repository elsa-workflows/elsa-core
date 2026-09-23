using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.Sqlite.Migrations.Secrets
{
    /// <inheritdoc />
    public partial class ManagedSecretOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ManagedGenerationId",
                schema: "Elsa",
                table: "Secrets",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagedOwnerId",
                schema: "Elsa",
                table: "Secrets",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("This migration is irreversible because dropping ownership markers could make lifecycle credential material mutable.");
        }
    }
}
