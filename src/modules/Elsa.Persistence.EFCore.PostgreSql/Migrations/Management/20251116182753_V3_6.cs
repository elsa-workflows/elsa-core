using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.PostgreSql.Migrations.Management
{
    /// <inheritdoc />
    public partial class V3_6 : Migration
    {
        private readonly Elsa.Persistence.EFCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_6(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalSource",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                type: "text",
                nullable: true);

            var schema = _schema.Schema;
            migrationBuilder.Sql($@"
                ALTER TABLE ""{schema}"".""WorkflowDefinitions""
                ALTER COLUMN ""StringData""
                TYPE jsonb
                USING ""StringData""::jsonb;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var schema = _schema.Schema;
            migrationBuilder.Sql($@"
                ALTER TABLE ""{schema}"".""WorkflowDefinitions""
                ALTER COLUMN ""StringData""
                TYPE text
                USING ""StringData""::text;
            ");

            migrationBuilder.DropColumn(
                name: "OriginalSource",
                schema: _schema.Schema,
                table: "WorkflowDefinitions");
        }
    }
}
