namespace Elsa.AI.Abstractions.Models;

public record AiContextAttachment
{
    public string? Id { get; init; }
    public string? ConversationId { get; init; }
    public string Kind { get; init; } = default!;
    public string? ReferenceId { get; init; }
    public string? TenantId { get; init; }
    public string? Scope { get; init; }
    public AiTimeRange? TimeRange { get; init; }
    public string? ActivityId { get; init; }
    public JsonObject Metadata { get; init; } = [];
}

public record AiResolvedContext
{
    public string Kind { get; init; } = default!;
    public string? ReferenceId { get; init; }
    public string Summary { get; init; } = "";
    public JsonObject Data { get; init; } = [];
    public JsonObject Metadata { get; init; } = [];
}

public record AiContextResolutionRequest
{
    public AiContextAttachment Attachment { get; init; } = default!;
    public string? TenantId { get; init; }
    public string UserId { get; init; } = default!;
}

public record AiTimeRange
{
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}
