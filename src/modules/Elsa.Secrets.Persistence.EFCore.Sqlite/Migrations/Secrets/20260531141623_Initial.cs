using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.Sqlite.Migrations.Secrets
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "Secrets",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    TypeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    StoreName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Versions = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secrets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Name",
                schema: "Elsa",
                table: "Secrets",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Scope",
                schema: "Elsa",
                table: "Secrets",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Status",
                schema: "Elsa",
                table: "Secrets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_StoreName",
                schema: "Elsa",
                table: "Secrets",
                column: "StoreName");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TypeName",
                schema: "Elsa",
                table: "Secrets",
                column: "TypeName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Secrets",
                schema: "Elsa");
        }
    }
}
