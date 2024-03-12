using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.MySql.Migrations.Management
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
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_IsSystem",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsSystem",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                column: "IsSystem");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowInstance_IsSystem",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowDefinition_IsSystem",
                schema: "Elsa",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "DataCompressionAlgorithm",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                schema: "Elsa",
                table: "WorkflowDefinitions");
        }
    }
}
