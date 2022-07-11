using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.CustomActivities.EntityFrameworkCore.Sqlite.Migrations
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
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
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
                name: "IX_ActivityDefinition_Name",
                table: "ActivityDefinitions",
                column: "Name");

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
