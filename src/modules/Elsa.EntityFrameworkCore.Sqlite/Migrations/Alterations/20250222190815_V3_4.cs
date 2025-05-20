using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Alterations
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
