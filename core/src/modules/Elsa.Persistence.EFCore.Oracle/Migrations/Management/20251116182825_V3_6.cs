using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Oracle.Migrations.Management
{
    /// <inheritdoc />
    /// <remarks>
    /// Oracle rejects an in-place <c>ALTER TABLE ... MODIFY</c> that changes a column's datatype to or from a LOB type
    /// (ORA-22858 / ORA-22859), so <c>StringData</c> is converted between NCLOB and JSON with the add/copy/drop/rename
    /// sequence <see cref="MigrationHelper.ConvertColumnType"/> emits. Because Oracle commits DDL implicitly, a run
    /// that fails partway leaves its earlier statements applied; every step here is therefore guarded so that a re-run
    /// picks up where the failed one stopped instead of failing on an already-added column.
    /// </remarks>
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
            MigrationHelper.AddColumnIfMissing(migrationBuilder, _schema, "WorkflowDefinitions", "OriginalSource", "NCLOB");

            MigrationHelper.ConvertColumnType(
                migrationBuilder,
                _schema,
                table: "WorkflowDefinitions",
                column: "StringData",
                fromDataType: "NCLOB",
                toDataType: "JSON",
                toColumnDefinition: "JSON",
                copyExpression: source => $"JSON(TO_CLOB({source}))",
                notNull: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MigrationHelper.ConvertColumnType(
                migrationBuilder,
                _schema,
                table: "WorkflowDefinitions",
                column: "StringData",
                fromDataType: "JSON",
                toDataType: "NCLOB",
                toColumnDefinition: "NCLOB",
                copyExpression: source => $"TO_NCLOB(JSON_SERIALIZE({source} RETURNING CLOB))",
                notNull: false);

            MigrationHelper.DropColumnIfPresent(migrationBuilder, _schema, "WorkflowDefinitions", "OriginalSource");
        }
    }
}
