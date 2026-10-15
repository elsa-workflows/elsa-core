using System;
using Elsa.Persistence.EFCore;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.AI.Persistence.EFCore.Sqlite.Migrations.AI
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        private readonly IElsaDbContextSchema _schema;

        public Initial(IElsaDbContextSchema schema)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: _schema.Schema);

            migrationBuilder.CreateTable(
                name: "AIAuditRecords",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true),
                    ActorId = table.Column<string>(type: "TEXT", nullable: false),
                    ConversationId = table.Column<string>(type: "TEXT", nullable: true),
                    ProposalId = table.Column<string>(type: "TEXT", nullable: true),
                    ToolInvocationId = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<string>(type: "TEXT", nullable: false),
                    TraceId = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: false),
                    Data = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAuditRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIConversations",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    RetentionMode = table.Column<string>(type: "TEXT", nullable: false),
                    RetentionExpiresAt = table.Column<string>(type: "TEXT", nullable: true),
                    Messages = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIProposals",
                schema: _schema.Schema,
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true),
                    ConversationId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    BaselineWorkflowDefinitionId = table.Column<string>(type: "TEXT", nullable: true),
                    BaselineVersionId = table.Column<string>(type: "TEXT", nullable: true),
                    WorkflowPayload = table.Column<string>(type: "TEXT", nullable: false),
                    Rationale = table.Column<string>(type: "TEXT", nullable: false),
                    Warnings = table.Column<string>(type: "TEXT", nullable: false),
                    ValidationDiagnostics = table.Column<string>(type: "TEXT", nullable: false),
                    GraphDiff = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "TEXT", nullable: false),
                    ReviewedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<string>(type: "TEXT", nullable: true),
                    AppliedBy = table.Column<string>(type: "TEXT", nullable: true),
                    AppliedAt = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIProposals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIAuditRecords_ActorId",
                schema: _schema.Schema,
                table: "AIAuditRecords",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AIAuditRecords_ProposalId",
                schema: _schema.Schema,
                table: "AIAuditRecords",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_AIAuditRecords_TenantId_ConversationId",
                schema: _schema.Schema,
                table: "AIAuditRecords",
                columns: new[] { "TenantId", "ConversationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AIAuditRecords_TenantId_Timestamp",
                schema: _schema.Schema,
                table: "AIAuditRecords",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AIAuditRecords_ToolInvocationId",
                schema: _schema.Schema,
                table: "AIAuditRecords",
                column: "ToolInvocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_RetentionExpiresAt",
                schema: _schema.Schema,
                table: "AIConversations",
                column: "RetentionExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_Status",
                schema: _schema.Schema,
                table: "AIConversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AIConversations_TenantId_UserId",
                schema: _schema.Schema,
                table: "AIConversations",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AIProposals_Status",
                schema: _schema.Schema,
                table: "AIProposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AIProposals_TenantId_ConversationId",
                schema: _schema.Schema,
                table: "AIProposals",
                columns: new[] { "TenantId", "ConversationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIAuditRecords",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "AIConversations",
                schema: _schema.Schema);

            migrationBuilder.DropTable(
                name: "AIProposals",
                schema: _schema.Schema);
        }
    }
}
