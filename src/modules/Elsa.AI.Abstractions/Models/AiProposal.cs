namespace Elsa.AI.Abstractions.Models;

public record AiProposal
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string? TenantId { get; init; }
    public string ConversationId { get; init; } = default!;
    public AiProposalKind Kind { get; init; }
    public AiProposalStatus Status { get; init; } = AiProposalStatus.Draft;
    public string? BaselineWorkflowDefinitionId { get; init; }
    public string? BaselineVersionId { get; init; }
    public JsonObject WorkflowPayload { get; init; } = [];
    public string Rationale { get; init; } = "";
    public ICollection<string> Warnings { get; init; } = [];
    public ICollection<AiValidationDiagnostic> ValidationDiagnostics { get; init; } = [];
    public AiGraphDiff? GraphDiff { get; init; }
    public string CreatedBy { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
    public string? ReviewedBy { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public string? AppliedBy { get; init; }
    public DateTimeOffset? AppliedAt { get; init; }
}

public record AiValidationDiagnostic
{
    public string Code { get; init; } = default!;
    public string Message { get; init; } = "";
    public AiValidationSeverity Severity { get; init; } = AiValidationSeverity.Warning;
    public string? Path { get; init; }
}

public record AiGraphDiff
{
    public ICollection<string> AddedActivityIds { get; init; } = [];
    public ICollection<string> RemovedActivityIds { get; init; } = [];
    public ICollection<string> ChangedActivityIds { get; init; } = [];
    public JsonObject Data { get; init; } = [];
}

public enum AiProposalKind
{
    WorkflowCreate,
    WorkflowUpdate
}

public enum AiProposalStatus
{
    Draft,
    Validated,
    Blocked,
    Approved,
    Rejected,
    Applied,
    Expired
}

public enum AiValidationSeverity
{
    Information,
    Warning,
    Error
}
