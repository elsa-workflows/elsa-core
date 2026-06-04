namespace Elsa.AI.Abstractions.Models;

public record AIContextAttachment
{
    public string? Id { get; init; }
    public string? ConversationId { get; init; }
    public string Kind { get; init; } = default!;
    public string? ReferenceId { get; init; }
    public string? TenantId { get; init; }
    public string? Scope { get; init; }
    public AITimeRange? TimeRange { get; init; }
    public string? ActivityId { get; init; }
    public JsonObject Metadata { get; init; } = [];
}

public record AIResolvedContext
{
    public string Kind { get; init; } = default!;
    public string? ReferenceId { get; init; }
    public string Summary { get; init; } = "";
    public JsonObject Data { get; init; } = [];
    public JsonObject Metadata { get; init; } = [];
}

public record AIContextResolutionRequest
{
    public AIContextAttachment Attachment { get; init; } = default!;
    public string? TenantId { get; init; }
    public string UserId { get; init; } = default!;
}

public record AITimeRange
{
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}
