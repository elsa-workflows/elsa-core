using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.SqlServer.Migrations.ActivityDefinitions
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "ActivityDefinitions",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DefinitionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsLatest = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_DefinitionId_Version",
                schema: "Elsa",
                table: "ActivityDefinitions",
                columns: new[] { "DefinitionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_IsLatest",
                schema: "Elsa",
                table: "ActivityDefinitions",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_IsPublished",
                schema: "Elsa",
                table: "ActivityDefinitions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_Type",
                schema: "Elsa",
                table: "ActivityDefinitions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityDefinition_Version",
                schema: "Elsa",
                table: "ActivityDefinitions",
                column: "Version");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDefinitions",
                schema: "Elsa");
        }
    }
}
