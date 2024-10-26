using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.PostgreSql.Migrations.Management
{
    /// <inheritdoc />
    public partial class V3_3 : Migration
    {
        private readonly Elsa.EntityFrameworkCore.IElsaDbContextSchema _schema;

        /// <inheritdoc />
        public V3_3(Elsa.EntityFrameworkCore.IElsaDbContextSchema schema)
        {
            _schema = schema;
        }

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Elsa");

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
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
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: true),
                    UsableAsActivity = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsLatest = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    DefinitionId = table.Column<string>(type: "text", nullable: false),
                    DefinitionVersionId = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ParentWorkflowInstanceId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubStatus = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    IncidentCount = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Data = table.Column<string>(type: "text", nullable: true),
                    DataCompressionAlgorithm = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_DefinitionId_Version",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                columns: new[] { "DefinitionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsLatest",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsPublished",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsSystem",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_Name",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_TenantId",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_UsableAsActivity",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                column: "UsableAsActivity");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_Version",
                schema: _schema.Schema,
                table: "WorkflowDefinitions",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_CorrelationId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_CreatedAt",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_DefinitionId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_FinishedAt",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "FinishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_IsSystem",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Name",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_DefinitionId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                columns: new[] { "Status", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_SubStatus",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                columns: new[] { "Status", "SubStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_SubStatus_DefinitionId_Version",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                columns: new[] { "Status", "SubStatus", "DefinitionId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_SubStatus",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "SubStatus");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_SubStatus_DefinitionId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                columns: new[] { "SubStatus", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_TenantId",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_UpdatedAt",
                schema: _schema.Schema,
                table: "WorkflowInstances",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowDefinitions",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "WorkflowInstances",
                schema: _schema.Schema);
        }
    }
}
