using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.ActivityDefinitions
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
                name: "ActivityDefinitions",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DefinitionId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityDefinitions",
                schema: "Elsa");
        }
    }
}
