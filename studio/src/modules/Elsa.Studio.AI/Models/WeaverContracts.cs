using System.Text.Json.Nodes;

namespace Elsa.Studio.AI.Models;

public record WeaverCapabilities(
    bool Streaming,
    bool ConversationPersistence,
    bool ProposalReview,
    IReadOnlyCollection<string> SupportedAttachmentKinds,
    IReadOnlyCollection<WeaverAgentCapability> Agents)
{
    public bool SupportsAttachmentKind(string kind) => SupportedAttachmentKinds.Any(x => string.Equals(x, kind, StringComparison.OrdinalIgnoreCase));
}

public record WeaverAgentCapability(string Name, string DisplayName, string Description);

public record WeaverToolDefinition
{
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public JsonObject Schema { get; init; } = [];
    public WeaverToolMutability Mutability { get; init; } = WeaverToolMutability.ReadOnly;
    public WeaverToolDangerLevel DangerLevel { get; init; } = WeaverToolDangerLevel.Low;
    public ICollection<string> Permissions { get; init; } = [];
    public WeaverTenantBehavior TenantBehavior { get; init; } = WeaverTenantBehavior.TenantScoped;
    public WeaverToolAuditBehavior AuditBehavior { get; init; } = WeaverToolAuditBehavior.RecordInvocation;
    public ICollection<string> AgentScopes { get; init; } = [];
    public ICollection<string> TenantIds { get; init; } = [];
    public ICollection<string> ActorIds { get; init; } = [];
    public string? Provider { get; init; }
    public bool EnabledByDefault { get; init; }
    public bool IsEnabled { get; init; }
}

public record WeaverChatRequest
{
    public string? ConversationId { get; init; }
    public string Message { get; init; } = "";
    public string? Agent { get; init; }
    public ICollection<WeaverContextAttachment> Attachments { get; init; } = [];
}

public record WeaverContextAttachment
{
    public string? Id { get; init; }
    public string Kind { get; init; } = "";
    public string? ReferenceId { get; init; }
    public string? Scope { get; init; }
    public WeaverTimeRange? TimeRange { get; init; }
    public string? ActivityId { get; init; }
    public JsonObject Metadata { get; init; } = [];
}

public record WeaverTimeRange(DateTimeOffset From, DateTimeOffset To);

public record WeaverStreamEvent
{
    public string Type { get; init; } = "";
    public string ConversationId { get; init; } = "";
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public JsonObject Data { get; init; } = [];
}

public enum WeaverToolMutability
{
    ReadOnly,
    Proposal,
    Administrative
}

public enum WeaverToolDangerLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum WeaverTenantBehavior
{
    TenantScoped,
    HostScoped,
    CrossTenantDenied
}

public enum WeaverToolAuditBehavior
{
    None,
    RecordInvocation,
    RecordInvocationAndResult
}
