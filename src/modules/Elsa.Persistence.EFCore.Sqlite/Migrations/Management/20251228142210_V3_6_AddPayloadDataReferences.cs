using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.Sqlite.Migrations.Management
{
    /// <inheritdoc />
    public partial class V3_6_AddPayloadDataReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DataReference_CompressionAlgorithm",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataReference_TypeIdentifier",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataReference_Url",
                schema: "Elsa",
                table: "WorkflowInstances",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataReference_CompressionAlgorithm",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataReference_TypeIdentifier",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "TEXT",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataReference_Url",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataReference_CompressionAlgorithm",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "DataReference_TypeIdentifier",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "DataReference_Url",
                schema: "Elsa",
                table: "WorkflowInstances");

            migrationBuilder.DropColumn(
                name: "DataReference_CompressionAlgorithm",
                schema: "Elsa",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "DataReference_TypeIdentifier",
                schema: "Elsa",
                table: "WorkflowDefinitions");

            migrationBuilder.DropColumn(
                name: "DataReference_Url",
                schema: "Elsa",
                table: "WorkflowDefinitions");
        }
    }
}
