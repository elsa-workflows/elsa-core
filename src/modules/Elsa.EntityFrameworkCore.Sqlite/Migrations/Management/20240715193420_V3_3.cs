#nullable disable

using Elsa.EntityFrameworkCore.Common.Contracts;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Management
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
                table: "WorkflowInstances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "WorkflowDefinitions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_TenantId",
                table: "WorkflowInstances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_TenantId",
                table: "WorkflowDefinitions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_TenantId",
                table: "WorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowDefinition_TenantId",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "WorkflowDefinitions");
        }
    }
}