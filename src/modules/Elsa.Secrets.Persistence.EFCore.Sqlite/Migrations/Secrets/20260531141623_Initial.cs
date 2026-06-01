using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Secrets.Persistence.EFCore.Sqlite.Migrations.Secrets
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        public Initial(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: _schema.Schema);

            migrationBuilder.CreateTable(
                name: "Secrets",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
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
                name: "IX_Secret_NormalizedName",
                schema: _schema.Schema,
                table: "Secrets",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Scope",
                schema: _schema.Schema,
                table: "Secrets",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_Status",
                schema: _schema.Schema,
                table: "Secrets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_StoreName",
                schema: _schema.Schema,
                table: "Secrets",
                column: "StoreName");

            migrationBuilder.CreateIndex(
                name: "IX_Secret_TypeName",
                schema: _schema.Schema,
                table: "Secrets",
                column: "TypeName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Secrets",
                schema: _schema.Schema);
        }
    }
}
