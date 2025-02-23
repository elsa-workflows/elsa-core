using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Labels
{
    /// <inheritdoc />
    public partial class V3_4 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_4(Elsa.EntityFrameworkCore.IElsaDbContextSchema schema)
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
