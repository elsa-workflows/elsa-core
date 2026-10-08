using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Management
{
    /// <inheritdoc />
    public partial class WorkflowDefinitionRegistryGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionRegistryGenerations",
                schema: "Elsa",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Generation = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitionRegistryGenerations", x => x.TenantId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowDefinitionRegistryGenerations",
                schema: "Elsa");
        }
    }
}
