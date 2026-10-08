using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Labels
{
    /// <inheritdoc />
    public partial class V3_4 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_4(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitionLabels",
                newName: "WorkflowDefinitionLabels",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "Labels",
                newName: "Labels",
                newSchema: _schema.Schema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "WorkflowDefinitionLabels",
                schema: _schema.Schema,
                newName: "WorkflowDefinitionLabels");

            migrationBuilder.RenameTable(
                name: "Labels",
                schema: _schema.Schema,
                newName: "Labels");
        }
    }
}
