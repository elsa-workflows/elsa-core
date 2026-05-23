namespace Elsa.AI.Abstractions.Models;

public record AiAuditEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string? TenantId { get; init; }
    public string ActorId { get; init; } = "";
    public string? ConversationId { get; init; }
    public string? ProposalId { get; init; }
    public string? ToolInvocationId { get; init; }
    public string Type { get; init; } = "";
    public DateTimeOffset Timestamp { get; init; }
    public string? TraceId { get; init; }
    public string Summary { get; init; } = "";
    public JsonObject Data { get; init; } = [];
}
