#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Labels
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "WorkflowDefinitionLabel_TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "WorkflowDefinitionLabel_TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitionLabels");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: _schema.Schema,
                table: "Labels");
        }
    }
}