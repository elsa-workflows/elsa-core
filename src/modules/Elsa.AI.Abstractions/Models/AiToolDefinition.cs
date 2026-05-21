namespace Elsa.AI.Abstractions.Models;

public record AiToolDefinition
{
    public string Name { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string Description { get; init; } = "";
    public JsonObject Schema { get; init; } = [];
    public AiToolMutability Mutability { get; init; } = AiToolMutability.ReadOnly;
    public AiToolDangerLevel DangerLevel { get; init; } = AiToolDangerLevel.Low;
    public ICollection<string> Permissions { get; init; } = [];
    public AiTenantBehavior TenantBehavior { get; init; } = AiTenantBehavior.TenantScoped;
    public AiToolAuditBehavior AuditBehavior { get; init; } = AiToolAuditBehavior.RecordInvocation;
    public ICollection<string> AgentScopes { get; init; } = [];
    public ICollection<string> TenantIds { get; init; } = [];
    public ICollection<string> ActorIds { get; init; } = [];
    public string? Provider { get; init; }
    public bool EnabledByDefault { get; init; }
    public bool IsEnabled { get; init; }
}

public record AiToolInvocation
{
    public string Id { get; init; } = default!;
    public string ConversationId { get; init; } = default!;
    public string ToolName { get; init; } = default!;
    public JsonObject Arguments { get; init; } = [];
    public AiToolAuthorizationResult AuthorizationResult { get; init; } = AiToolAuthorizationResult.NotEvaluated;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public AiToolInvocationStatus Status { get; init; } = AiToolInvocationStatus.Pending;
    public string? ResultSummary { get; init; }
    public string? Error { get; init; }
    public string? TraceId { get; init; }
    public string? TenantId { get; init; }
    public string ActorId { get; init; } = default!;
}

public record AiToolResult
{
    public AiToolInvocationStatus Status { get; init; } = AiToolInvocationStatus.Completed;
    public string Summary { get; init; } = "";
    public JsonObject Data { get; init; } = [];
    public string? Error { get; init; }
}

public record AiToolExecutionContext
{
    public string ConversationId { get; init; } = default!;
    public string? TenantId { get; init; }
    public string ActorId { get; init; } = default!;
    public string? Agent { get; init; }
    public JsonObject Arguments { get; init; } = [];
}

public record AiToolQuery
{
    public string? Agent { get; init; }
    public AiToolMutability? Mutability { get; init; }
    public AiToolDangerLevel? DangerLevel { get; init; }
    public string? TenantId { get; init; }
    public string? ActorId { get; init; }
}

public enum AiToolMutability
{
    ReadOnly,
    Proposal,
    Administrative
}

public enum AiToolDangerLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum AiTenantBehavior
{
    TenantScoped,
    HostScoped,
    CrossTenantDenied
}

public enum AiToolAuditBehavior
{
    None,
    RecordInvocation,
    RecordInvocationAndResult
}

public enum AiToolAuthorizationResult
{
    NotEvaluated,
    Allowed,
    Denied
}

public enum AiToolInvocationStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Denied
}
