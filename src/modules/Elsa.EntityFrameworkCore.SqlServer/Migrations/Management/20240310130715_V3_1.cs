using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.SqlServer.Migrations.Management
{
    /// <inheritdoc />
    public partial class V3_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataCompressionAlgorithm",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_TenantId",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_TenantId",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_TenantId",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowDefinition_TenantId",
                schema: "Elsa",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "DataCompressionAlgorithm",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "Elsa",
                table: "WorkflowDefinitions");
        }
    }
}
