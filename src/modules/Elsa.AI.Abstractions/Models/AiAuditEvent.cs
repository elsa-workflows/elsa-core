namespace Elsa.AI.Abstractions.Models;

public record AiAuditEvent
{
    public string Id { get; init; } = default!;
    public string? TenantId { get; init; }
    public string ActorId { get; init; } = default!;
    public string? ConversationId { get; init; }
    public string? ProposalId { get; init; }
    public string? ToolInvocationId { get; init; }
    public string Type { get; init; } = default!;
    public DateTimeOffset Timestamp { get; init; }
    public string? TraceId { get; init; }
    public string Summary { get; init; } = "";
    public JsonObject Data { get; init; } = [];
}
