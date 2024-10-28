using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.EntityFrameworkCore.Sqlite.Migrations.Management
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
            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ToolVersion = table.Column<string>(type: "TEXT", nullable: true),
                    ProviderName = table.Column<string>(type: "TEXT", nullable: true),
                    MaterializerName = table.Column<string>(type: "TEXT", nullable: false),
                    MaterializerContext = table.Column<string>(type: "TEXT", nullable: true),
                    StringData = table.Column<string>(type: "TEXT", nullable: true),
                    BinaryData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    IsReadonly = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    UsableAsActivity = table.Column<bool>(type: "INTEGER", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLatest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionId = table.Column<string>(type: "TEXT", nullable: false),
                    DefinitionVersionId = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentWorkflowInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    SubStatus = table.Column<string>(type: "TEXT", nullable: false),
                    CorrelationId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IncidentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    DataCompressionAlgorithm = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_DefinitionId_Version",
                table: "WorkflowDefinitions",
                columns: new[] { "DefinitionId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsLatest",
                table: "WorkflowDefinitions",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsPublished",
                table: "WorkflowDefinitions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsSystem",
                table: "WorkflowDefinitions",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_Name",
                table: "WorkflowDefinitions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_TenantId",
                table: "WorkflowDefinitions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_UsableAsActivity",
                table: "WorkflowDefinitions",
                column: "UsableAsActivity");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_Version",
                table: "WorkflowDefinitions",
                column: "Version");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_CorrelationId",
                table: "WorkflowInstances",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_CreatedAt",
                table: "WorkflowInstances",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_DefinitionId",
                table: "WorkflowInstances",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_FinishedAt",
                table: "WorkflowInstances",
                column: "FinishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_IsSystem",
                table: "WorkflowInstances",
                column: "IsSystem");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Name",
                table: "WorkflowInstances",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status",
                table: "WorkflowInstances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_DefinitionId",
                table: "WorkflowInstances",
                columns: new[] { "Status", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_SubStatus",
                table: "WorkflowInstances",
                columns: new[] { "Status", "SubStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_SubStatus_DefinitionId_Version",
                table: "WorkflowInstances",
                columns: new[] { "Status", "SubStatus", "DefinitionId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_SubStatus",
                table: "WorkflowInstances",
                column: "SubStatus");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_SubStatus_DefinitionId",
                table: "WorkflowInstances",
                columns: new[] { "SubStatus", "DefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_TenantId",
                table: "WorkflowInstances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_UpdatedAt",
                table: "WorkflowInstances",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowDefinitions");

            migrationBuilder.DropTable(
                name: "WorkflowInstances");
        }
    }
}
