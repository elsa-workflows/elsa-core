using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.AI.Persistence.EFCore.Migrations;

[DbContext(typeof(AiDbContext))]
[Migration("20260521002000_Initial")]
public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AiAuditRecords",
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
                table.PrimaryKey("PK_AiAuditRecords", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AiProposals",
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
                table.PrimaryKey("PK_AiProposals", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AiAuditRecords_ProposalId",
            table: "AiAuditRecords",
            column: "ProposalId");

        migrationBuilder.CreateIndex(
            name: "IX_AiAuditRecords_TenantId_ConversationId",
            table: "AiAuditRecords",
            columns: new[] { "TenantId", "ConversationId" });

        migrationBuilder.CreateIndex(
            name: "IX_AiAuditRecords_ToolInvocationId",
            table: "AiAuditRecords",
            column: "ToolInvocationId");

        migrationBuilder.CreateIndex(
            name: "IX_AiProposals_Status",
            table: "AiProposals",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_AiProposals_TenantId_ConversationId",
            table: "AiProposals",
            columns: new[] { "TenantId", "ConversationId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AiAuditRecords");
        migrationBuilder.DropTable(name: "AiProposals");
    }
}
