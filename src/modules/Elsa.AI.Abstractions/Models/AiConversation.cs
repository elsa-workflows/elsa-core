namespace Elsa.AI.Abstractions.Models;

public record AiConversation
{
    public string Id { get; init; } = default!;
    public string? TenantId { get; init; }
    public string UserId { get; init; } = default!;
    public string? Title { get; init; }
    public AiConversationStatus Status { get; init; } = AiConversationStatus.Active;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? ProviderSessionId { get; init; }
    public AiRetentionMode RetentionMode { get; init; } = AiRetentionMode.Configured;
    public DateTimeOffset? RetentionExpiresAt { get; init; }
    public IReadOnlyCollection<AiMessage> Messages { get; init; } = [];
}

public record AiMessage
{
    public string Id { get; init; } = default!;
    public string ConversationId { get; init; } = default!;
    public AiMessageRole Role { get; init; }
    public string Content { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public long StreamSequence { get; init; }
    public JsonObject Metadata { get; init; } = [];
}

public record AiChatRequest
{
    public string? ConversationId { get; init; }
    public string Message { get; init; } = "";
    public string? Agent { get; init; }
    public string? ProviderName { get; init; }
    public ICollection<AiContextAttachment> Attachments { get; init; } = [];
    public string? TenantId { get; init; }
    public string UserId { get; init; } = default!;
    public ICollection<string> UserPermissions { get; init; } = [];
}

public record AiStreamEvent
{
    public string Type { get; init; } = default!;
    public string ConversationId { get; init; } = default!;
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public JsonObject Data { get; init; } = [];
}

public record AiSessionHandle
{
    public string Id { get; init; } = default!;
    public string? ProviderSessionId { get; init; }
}

public record CreateAiSessionRequest
{
    public string ConversationId { get; init; } = default!;
    public string? Agent { get; init; }
    public string? TenantId { get; init; }
    public JsonObject Metadata { get; init; } = [];
}

public record AiTurnRequest
{
    public string ConversationId { get; init; } = default!;
    public string Message { get; init; } = "";
    public IReadOnlyCollection<AiMessage> Messages { get; init; } = [];
    public IReadOnlyCollection<AiResolvedContext> Context { get; init; } = [];
    public IReadOnlyCollection<AiToolDefinition> Tools { get; init; } = [];
    public IReadOnlyCollection<AiToolTurnResult> ToolResults { get; init; } = [];
    public string? Agent { get; init; }
}

public record AiToolTurnResult
{
    public string ToolCallId { get; init; } = default!;
    public string ToolName { get; init; } = default!;
    public AiToolResult Result { get; init; } = new();
}

public record AiProviderEvent
{
    public string Type { get; init; } = default!;
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public JsonObject Data { get; init; } = [];
}

public enum AiConversationStatus
{
    Active,
    Completed,
    Failed,
    Expired
}

public enum AiRetentionMode
{
    Configured,
    Ephemeral,
    Durable
}

public enum AiMessageRole
{
    User,
    Assistant,
    System,
    Tool
}
