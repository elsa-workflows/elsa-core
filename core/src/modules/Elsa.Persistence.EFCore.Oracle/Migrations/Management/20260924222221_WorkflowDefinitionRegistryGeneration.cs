using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Management
{
    /// <inheritdoc />
    public partial class WorkflowDefinitionRegistryGeneration : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public WorkflowDefinitionRegistryGeneration(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitionRegistryGenerations",
                schema: _schema.Schema,
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "NVARCHAR2(450)", maxLength: 450, nullable: false),
                    Generation = table.Column<long>(type: "NUMBER(19)", nullable: false)
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
                schema: _schema.Schema);

        }
    }
}