namespace Elsa.AI.Persistence.EFCore.Entities;

public class AIConversationRecord
{
    public string Id { get; set; } = default!;
    public string? TenantId { get; set; }
    public string UserId { get; set; } = default!;
    public string? Title { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? ProviderSessionId { get; set; }
    public string RetentionMode { get; set; } = default!;
    public DateTimeOffset? RetentionExpiresAt { get; set; }
    public string Messages { get; set; } = "[]";
}
