using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Management
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DefinitionId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ToolVersion = table.Column<string>(type: "text", nullable: true),
                    ProviderName = table.Column<string>(type: "text", nullable: true),
                    MaterializerName = table.Column<string>(type: "text", nullable: false),
                    MaterializerContext = table.Column<string>(type: "text", nullable: true),
                    StringData = table.Column<string>(type: "text", nullable: true),
                    BinaryData = table.Column<byte[]>(type: "bytea", nullable: true),
                    IsReadonly = table.Column<bool>(type: "boolean", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: true),
                    UsableAsActivity = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                schema: "Elsa",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DefinitionId = table.Column<string>(type: "text", nullable: false),
                    DefinitionVersionId = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubStatus = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_DefinitionId_Version",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                columns: new[] { "DefinitionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsLatest",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsPublished",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_Name",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_UsableAsActivity",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                column: "UsableAsActivity");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_Version",
                schema: "Elsa",
                table: "WorkflowDefinitions",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_CorrelationId",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_CreatedAt",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_DefinitionId",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_FinishedAt",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "FinishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Name",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_DefinitionId",
                schema: "Elsa",
                table: "WorkflowInstances",
                columns: new[] { "Status", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_SubStatus",
                schema: "Elsa",
                table: "WorkflowInstances",
                columns: new[] { "Status", "SubStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_SubStatus_DefinitionId_Version",
                schema: "Elsa",
                table: "WorkflowInstances",
                columns: new[] { "Status", "SubStatus", "DefinitionId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_SubStatus",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "SubStatus");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_SubStatus_DefinitionId",
                schema: "Elsa",
                table: "WorkflowInstances",
                columns: new[] { "SubStatus", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_UpdatedAt",
                schema: "Elsa",
                table: "WorkflowInstances",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowDefinitions",
                schema: "Elsa");

            migrationBuilder.DropTable(
                name: "WorkflowInstances",
                schema: "Elsa");
        }
    }
}
