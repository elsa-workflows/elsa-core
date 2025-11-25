using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.MySql.Migrations.Management
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
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "StringData",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                type: "JSON",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StringData",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "JSON",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.DropColumn(
                name: "OriginalSource",
                schema: _schema.Schema,
                table: "WorkflowDefinitions");
        }
    }
}
