namespace Elsa.AI.Abstractions.Models;

public record AIProposal
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string? TenantId { get; init; }
    public string ConversationId { get; init; } = "";
    public AIProposalKind Kind { get; init; }
    public AIProposalStatus Status { get; init; } = AIProposalStatus.Draft;
    public string? BaselineWorkflowDefinitionId { get; init; }
    public string? BaselineVersionId { get; init; }
    public JsonObject WorkflowPayload { get; init; } = [];
    public string Rationale { get; init; } = "";
    public ICollection<string> Warnings { get; init; } = [];
    public ICollection<AIValidationDiagnostic> ValidationDiagnostics { get; init; } = [];
    public AIGraphDiff? GraphDiff { get; init; }
    public string CreatedBy { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? ReviewedBy { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public string? AppliedBy { get; init; }
    public DateTimeOffset? AppliedAt { get; init; }
}

public record AIValidationDiagnostic
{
    public string Code { get; init; } = default!;
    public string Message { get; init; } = "";
    public AIValidationSeverity Severity { get; init; } = AIValidationSeverity.Warning;
    public string? Path { get; init; }
}

public record AIGraphDiff
{
    public ICollection<string> AddedActivityIds { get; init; } = [];
    public ICollection<string> RemovedActivityIds { get; init; } = [];
    public ICollection<string> ChangedActivityIds { get; init; } = [];
    public JsonObject Data { get; init; } = [];
}

public enum AIProposalKind
{
    WorkflowCreate,
    WorkflowUpdate
}

public enum AIProposalStatus
{
    Draft,
    Validated,
    Blocked,
    Approved,
    Rejected,
    Applied,
    Expired
}

public enum AIValidationSeverity
{
    Information,
    Warning,
    Error
}
