using Elsa.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Identity
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
                name: "Users",
                newName: "Users",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Roles",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "Applications",
                newName: "Applications",
                newSchema: _schema.Schema);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Users",
                schema: _schema.Schema,
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: _schema.Schema,
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "Applications",
                schema: _schema.Schema,
                newName: "Applications");
        }
    }
}
