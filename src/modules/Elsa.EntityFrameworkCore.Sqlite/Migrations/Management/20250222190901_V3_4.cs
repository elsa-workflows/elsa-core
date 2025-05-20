using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Management
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
                name: "WorkflowInstances",
                newName: "WorkflowInstances",
                newSchema: _schema.Schema);

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitions",
                newName: "WorkflowDefinitions",
                newSchema: _schema.Schema);

            migrationBuilder.AddColumn<bool>(
                name: "IsExecuting",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_IsExecuting",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "IsExecuting");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_IsExecuting",
                schema: _schema.Schema,
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "IsExecuting",
                schema: _schema.Schema,
                table: "WorkflowInstances");

            migrationBuilder.RenameTable(
                name: "WorkflowInstances",
                schema: _schema.Schema,
                newName: "WorkflowInstances");

            migrationBuilder.RenameTable(
                name: "WorkflowDefinitions",
                schema: _schema.Schema,
                newName: "WorkflowDefinitions");
        }
    }
}
