using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.AI.Persistence.EFCore.Migrations;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AIAuditRecords",
            columns: table => new
            {
                Id = table.Column<string>(nullable: false),
                TenantId = table.Column<string>(nullable: true),
                ActorId = table.Column<string>(nullable: false),
                ConversationId = table.Column<string>(nullable: true),
                ProposalId = table.Column<string>(nullable: true),
                ToolInvocationId = table.Column<string>(nullable: true),
                Type = table.Column<string>(nullable: false),
                Timestamp = table.Column<DateTimeOffset>(nullable: false),
                TraceId = table.Column<string>(nullable: true),
                Summary = table.Column<string>(nullable: false),
                Data = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AIAuditRecords", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AIConversations",
            columns: table => new
            {
                Id = table.Column<string>(nullable: false),
                TenantId = table.Column<string>(nullable: true),
                UserId = table.Column<string>(nullable: false),
                Title = table.Column<string>(nullable: true),
                Status = table.Column<string>(nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(nullable: false),
                ProviderSessionId = table.Column<string>(nullable: true),
                RetentionMode = table.Column<string>(nullable: false),
                RetentionExpiresAt = table.Column<DateTimeOffset>(nullable: true),
                Messages = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AIConversations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AIProposals",
            columns: table => new
            {
                Id = table.Column<string>(nullable: false),
                TenantId = table.Column<string>(nullable: true),
                ConversationId = table.Column<string>(nullable: false),
                Kind = table.Column<string>(nullable: false),
                Status = table.Column<string>(nullable: false),
                BaselineWorkflowDefinitionId = table.Column<string>(nullable: true),
                BaselineVersionId = table.Column<string>(nullable: true),
                WorkflowPayload = table.Column<string>(nullable: false),
                Rationale = table.Column<string>(nullable: false),
                Warnings = table.Column<string>(nullable: false),
                ValidationDiagnostics = table.Column<string>(nullable: false),
                GraphDiff = table.Column<string>(nullable: true),
                CreatedBy = table.Column<string>(nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                ReviewedBy = table.Column<string>(nullable: true),
                ReviewedAt = table.Column<DateTimeOffset>(nullable: true),
                AppliedBy = table.Column<string>(nullable: true),
                AppliedAt = table.Column<DateTimeOffset>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AIProposals", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AIAuditRecords_ProposalId",
            table: "AIAuditRecords",
            column: "ProposalId");

        migrationBuilder.CreateIndex(
            name: "IX_AIAuditRecords_ActorId",
            table: "AIAuditRecords",
            column: "ActorId");

        migrationBuilder.CreateIndex(
            name: "IX_AIAuditRecords_TenantId_Timestamp",
            table: "AIAuditRecords",
            columns: new[] { "TenantId", "Timestamp" });

        migrationBuilder.CreateIndex(
            name: "IX_AIAuditRecords_TenantId_ConversationId",
            table: "AIAuditRecords",
            columns: new[] { "TenantId", "ConversationId" });

        migrationBuilder.CreateIndex(
            name: "IX_AIAuditRecords_ToolInvocationId",
            table: "AIAuditRecords",
            column: "ToolInvocationId");

        migrationBuilder.CreateIndex(
            name: "IX_AIConversations_RetentionExpiresAt",
            table: "AIConversations",
            column: "RetentionExpiresAt");

        migrationBuilder.CreateIndex(
            name: "IX_AIConversations_Status",
            table: "AIConversations",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_AIConversations_TenantId_UserId",
            table: "AIConversations",
            columns: new[] { "TenantId", "UserId" });

        migrationBuilder.CreateIndex(
            name: "IX_AIProposals_Status",
            table: "AIProposals",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_AIProposals_TenantId_ConversationId",
            table: "AIProposals",
            columns: new[] { "TenantId", "ConversationId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AIAuditRecords");
        migrationBuilder.DropTable(name: "AIConversations");
        migrationBuilder.DropTable(name: "AIProposals");
    }
}
