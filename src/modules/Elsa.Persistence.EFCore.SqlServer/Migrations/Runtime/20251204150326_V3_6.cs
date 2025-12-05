using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.SqlServer.Migrations.Runtime
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
            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                columns: new[] { "WorkflowDefinitionId", "Hash", "ActivityId" },
                unique: true,
                filter: "[Hash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId",
                schema: _schema.Schema,
                table: "Triggers");

            migrationBuilder.AlterColumn<string>(
                name: "ActivityId",
                schema: _schema.Schema,
                table: "Triggers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
