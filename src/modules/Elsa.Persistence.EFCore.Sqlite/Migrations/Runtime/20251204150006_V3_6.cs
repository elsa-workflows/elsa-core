using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Runtime
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
            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                columns: new[] { "WorkflowDefinitionId", "Hash", "ActivityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId",
                schema: _schema.Schema,
                table: "Triggers");
        }
    }
}
