using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Alterations
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
                name: "AlterationPlans",
                newName: "AlterationPlans",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "AlterationJobs",
                newName: "AlterationJobs",
                newSchema: _schema.Schema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "AlterationPlans",
                schema: _schema.Schema,
                newName: "AlterationPlans");

            migrationBuilder.RenameTable(
                name: "AlterationJobs",
                schema: _schema.Schema,
                newName: "AlterationJobs");
        }
    }
}
