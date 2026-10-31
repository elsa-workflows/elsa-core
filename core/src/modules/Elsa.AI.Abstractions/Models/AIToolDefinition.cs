namespace Elsa.AI.Abstractions.Models;

public record AIToolDefinition
{
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public JsonObject Schema { get; init; } = [];
    public AIToolMutability Mutability { get; init; } = AIToolMutability.ReadOnly;
    public AIToolDangerLevel DangerLevel { get; init; } = AIToolDangerLevel.Low;
    public ICollection<string> Permissions { get; init; } = [];
    public AITenantBehavior TenantBehavior { get; init; } = AITenantBehavior.TenantScoped;
    public AIToolAuditBehavior AuditBehavior { get; init; } = AIToolAuditBehavior.RecordInvocation;
    public ICollection<string> AgentScopes { get; init; } = [];
    public ICollection<string> TenantIds { get; init; } = [];
    public ICollection<string> ActorIds { get; init; } = [];
    public string? Provider { get; init; }
    public bool EnabledByDefault { get; init; }
    public bool IsEnabled { get; init; }
}

public record AIToolInvocation
{
    public string Id { get; init; } = default!;
    public string ConversationId { get; init; } = default!;
    public string ToolName { get; init; } = default!;
    public JsonObject Arguments { get; init; } = [];
    public AIToolAuthorizationResult AuthorizationResult { get; init; } = AIToolAuthorizationResult.NotEvaluated;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public AIToolInvocationStatus Status { get; init; } = AIToolInvocationStatus.Pending;
    public string? ResultSummary { get; init; }
    public string? Error { get; init; }
    public string? TraceId { get; init; }
    public string? TenantId { get; init; }
    public string ActorId { get; init; } = default!;
}

public record AIToolResult
{
    public AIToolInvocationStatus Status { get; init; } = AIToolInvocationStatus.Completed;
    public string Summary { get; init; } = "";
    public JsonObject Data { get; init; } = [];
    public string? Error { get; init; }
}

public record AIToolExecutionContext
{
    public string ConversationId { get; init; } = default!;
    public string? TenantId { get; init; }
    public string ActorId { get; init; } = default!;
    public string? Agent { get; init; }
    public JsonObject Arguments { get; init; } = [];
}

public record AIToolQuery
{
    public string? Agent { get; init; }
    public AIToolMutability? Mutability { get; init; }
    public AIToolDangerLevel? DangerLevel { get; init; }
    public string? TenantId { get; init; }
    public string? ActorId { get; init; }
    public ICollection<string> UserPermissions { get; init; } = [];
}

public enum AIToolMutability
{
    ReadOnly,
    Proposal,
    Administrative
}

public enum AIToolDangerLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum AITenantBehavior
{
    TenantScoped,
    HostScoped,
    CrossTenantDenied
}

public enum AIToolAuditBehavior
{
    None,
    RecordInvocation,
    RecordInvocationAndResult
}

public enum AIToolAuthorizationResult
{
    NotEvaluated,
    Allowed,
    Denied
}

public enum AIToolInvocationStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Denied
}
