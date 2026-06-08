namespace Elsa.AI.Abstractions.Models;

public record AIGroundingToolResult
{
    public string Summary { get; init; } = "";
    public IReadOnlyCollection<JsonObject> Items { get; init; } = [];
    public int Total { get; init; }
    public int Returned { get; init; }
    public bool Truncated { get; init; }
    public string? Cursor { get; init; }
    public IReadOnlyCollection<string> Evidence { get; init; } = [];
    public IReadOnlyCollection<string> Warnings { get; init; } = [];
}

public record AIGroundingCapabilityDescriptor
{
    public string Family { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public bool Available { get; init; }
    public IReadOnlyCollection<string> ToolNames { get; init; } = [];
    public IReadOnlyCollection<string> AttachmentKinds { get; init; } = [];
    public IReadOnlyCollection<string> DisabledReasons { get; init; } = [];
}

public record ActivityGroundingSummary
{
    public string Type { get; init; } = "";
    public int Version { get; init; }
    public string Namespace { get; init; } = "";
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Description { get; init; }
    public string Category { get; init; } = "";
    public bool IsBrowsable { get; init; }
    public bool IsTrigger { get; init; }
    public bool IsContainer { get; init; }
    public bool IsTerminal { get; init; }
    public IReadOnlyCollection<ActivityPortSummary> Inputs { get; init; } = [];
    public IReadOnlyCollection<ActivityPortSummary> Outputs { get; init; } = [];
    public IReadOnlyCollection<string> Ports { get; init; } = [];
    public IReadOnlyCollection<string> Constraints { get; init; } = [];
}

public record ActivityPortSummary
{
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string? Description { get; init; }
    public string Type { get; init; } = "";
    public string? Category { get; init; }
    public bool IsBrowsable { get; init; } = true;
    public bool IsSensitive { get; init; }
    public bool IsRequired { get; init; }
    public string? UIHint { get; init; }
    public string? DefaultSyntax { get; init; }
}

public record WorkflowGroundingSummary
{
    public string Id { get; init; } = "";
    public string DefinitionId { get; init; } = "";
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? Version { get; init; }
    public bool IsLatest { get; init; }
    public bool IsPublished { get; init; }
    public bool IsReadonly { get; init; }
    public string MaterializerName { get; init; } = "";
    public string? ProviderName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyCollection<string> ActivityTypes { get; init; } = [];
    public IReadOnlyCollection<string> Variables { get; init; } = [];
    public IReadOnlyCollection<string> Inputs { get; init; } = [];
    public IReadOnlyCollection<string> Outputs { get; init; } = [];
}

public record RuntimeInstanceGroundingSummary
{
    public string Id { get; init; } = "";
    public string? TenantId { get; init; }
    public string DefinitionId { get; init; } = "";
    public string DefinitionVersionId { get; init; } = "";
    public int Version { get; init; }
    public string Status { get; init; } = "";
    public string SubStatus { get; init; } = "";
    public string? CorrelationId { get; init; }
    public string? Name { get; init; }
    public int IncidentCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
}

public record IncidentGroundingSummary
{
    public string WorkflowInstanceId { get; init; } = "";
    public string ActivityId { get; init; } = "";
    public string ActivityNodeId { get; init; } = "";
    public string ActivityType { get; init; } = "";
    public string Message { get; init; } = "";
    public string? ExceptionType { get; init; }
    public string? ExceptionMessage { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}
