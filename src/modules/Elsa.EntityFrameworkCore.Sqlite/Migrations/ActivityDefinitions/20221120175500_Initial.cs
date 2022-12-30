using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.ActivityDefinitions
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLatest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_DefinitionId_Version",
                table: "ActivityDefinitions",
                columns: new[] { "DefinitionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_IsLatest",
                table: "ActivityDefinitions",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_IsPublished",
                table: "ActivityDefinitions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_Type",
                table: "ActivityDefinitions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_Version",
                table: "ActivityDefinitions",
                column: "Version");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDefinitions");
        }
    }
}
