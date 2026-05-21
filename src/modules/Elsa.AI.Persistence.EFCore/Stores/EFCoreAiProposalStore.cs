using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;

namespace Elsa.AI.Persistence.EFCore.Stores;

public class EFCoreAiProposalStore(AiDbContext dbContext) : IAiProposalStore
{
    public async ValueTask<AiProposal?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Proposals.FindAsync([id], cancellationToken);
        return record == null ? null : Map(record);
    }

    public async ValueTask SaveAsync(AiProposal proposal, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Proposals.FindAsync([proposal.Id], cancellationToken);
        if (record == null)
        {
            record = new AiProposalRecord { Id = proposal.Id };
            dbContext.Proposals.Add(record);
        }

        Map(proposal, record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AiProposal Map(AiProposalRecord record) =>
        new()
        {
            Id = record.Id,
            TenantId = record.TenantId,
            ConversationId = record.ConversationId,
            Kind = Enum.Parse<AiProposalKind>(record.Kind, ignoreCase: true),
            Status = Enum.Parse<AiProposalStatus>(record.Status, ignoreCase: true),
            BaselineWorkflowDefinitionId = record.BaselineWorkflowDefinitionId,
            BaselineVersionId = record.BaselineVersionId,
            WorkflowPayload = JsonSerializer.Deserialize<JsonObject>(record.WorkflowPayload) ?? [],
            Rationale = record.Rationale,
            Warnings = JsonSerializer.Deserialize<ICollection<string>>(record.Warnings) ?? [],
            ValidationDiagnostics = JsonSerializer.Deserialize<ICollection<AiValidationDiagnostic>>(record.ValidationDiagnostics) ?? [],
            GraphDiff = record.GraphDiff == null ? null : JsonSerializer.Deserialize<AiGraphDiff>(record.GraphDiff),
            CreatedBy = record.CreatedBy,
            CreatedAt = record.CreatedAt,
            ReviewedBy = record.ReviewedBy,
            ReviewedAt = record.ReviewedAt,
            AppliedBy = record.AppliedBy,
            AppliedAt = record.AppliedAt
        };

    private static void Map(AiProposal proposal, AiProposalRecord record)
    {
        record.TenantId = proposal.TenantId;
        record.ConversationId = proposal.ConversationId;
        record.Kind = proposal.Kind.ToString();
        record.Status = proposal.Status.ToString();
        record.BaselineWorkflowDefinitionId = proposal.BaselineWorkflowDefinitionId;
        record.BaselineVersionId = proposal.BaselineVersionId;
        record.WorkflowPayload = proposal.WorkflowPayload.ToJsonString();
        record.Rationale = proposal.Rationale;
        record.Warnings = JsonSerializer.Serialize(proposal.Warnings);
        record.ValidationDiagnostics = JsonSerializer.Serialize(proposal.ValidationDiagnostics);
        record.GraphDiff = proposal.GraphDiff == null ? null : JsonSerializer.Serialize(proposal.GraphDiff);
        record.CreatedBy = proposal.CreatedBy;
        record.CreatedAt = proposal.CreatedAt;
        record.ReviewedBy = proposal.ReviewedBy;
        record.ReviewedAt = proposal.ReviewedAt;
        record.AppliedBy = proposal.AppliedBy;
        record.AppliedAt = proposal.AppliedAt;
    }
}
