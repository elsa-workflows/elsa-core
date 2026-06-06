namespace Elsa.AI.Persistence.EFCore.Entities;

public class AIProposalRecord
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
    public string ConversationId { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? BaselineWorkflowDefinitionId { get; set; }
    public string? BaselineVersionId { get; set; }
    public string WorkflowPayload { get; set; } = "{}";
    public string Rationale { get; set; } = "";
    public string Warnings { get; set; } = "[]";
    public string ValidationDiagnostics { get; set; } = "[]";
    public string? GraphDiff { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? AppliedBy { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
}
