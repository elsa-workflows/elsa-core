namespace Elsa.AI.Persistence.EFCore.Entities;

public class AiAuditRecord
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
    public string ActorId { get; set; } = default!;
    public string? ConversationId { get; set; }
    public string? ProposalId { get; set; }
    public string? ToolInvocationId { get; set; }
    public string Type { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; }
    public string? TraceId { get; set; }
    public string Summary { get; set; } = "";
    public string Data { get; set; } = "{}";
}
