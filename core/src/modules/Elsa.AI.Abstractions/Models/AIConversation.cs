namespace Elsa.AI.Abstractions.Models;

public record AIConversation
{
    public string Id { get; init; } = default!;
    public string? TenantId { get; init; }
    public string UserId { get; init; } = default!;
    public string? Title { get; init; }
    public AIConversationStatus Status { get; init; } = AIConversationStatus.Active;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public string? ProviderSessionId { get; init; }
    public AIRetentionMode RetentionMode { get; init; } = AIRetentionMode.Configured;
    public DateTimeOffset? RetentionExpiresAt { get; init; }
    public IReadOnlyCollection<AIMessage> Messages { get; init; } = [];
}

public record AIMessage
{
    public string Id { get; init; } = default!;
    public string ConversationId { get; init; } = default!;
    public AIMessageRole Role { get; init; }
    public string Content { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public long StreamSequence { get; init; }
    public JsonObject Metadata { get; init; } = [];
}

public record AIChatRequest
{
    public string? ConversationId { get; init; }
    public string Message { get; init; } = "";
    public string? Agent { get; init; }
    public string? ProviderName { get; init; }
    public ICollection<AIContextAttachment> Attachments { get; init; } = [];
    public string? TenantId { get; init; }
    public string UserId { get; init; } = default!;
    public ICollection<string> UserPermissions { get; init; } = [];
    public bool IsReconnect { get; init; }
}

public record AIStreamEvent
{
    public string Type { get; init; } = default!;
    public string ConversationId { get; init; } = default!;
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public JsonObject Data { get; init; } = [];
}

public record AISessionHandle
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string? ProviderSessionId { get; init; }
}

public record CreateAISessionRequest
{
    public string ConversationId { get; init; } = default!;
    public string? Agent { get; init; }
    public string? TenantId { get; init; }
    public AIProviderConfiguration? ProviderConfiguration { get; init; }
    public JsonObject Metadata { get; init; } = [];
}

public record AITurnRequest
{
    public string ConversationId { get; init; } = default!;
    public string? ProviderSessionId { get; init; }
    public string Message { get; init; } = "";
    public IReadOnlyCollection<AIMessage> Messages { get; init; } = [];
    public IReadOnlyCollection<AIResolvedContext> Context { get; init; } = [];
    public IReadOnlyCollection<AIToolDefinition> Tools { get; init; } = [];
    public string? Agent { get; init; }
    public AIProviderConfiguration? ProviderConfiguration { get; init; }
}

public record AIProviderConfiguration
{
    public string Name { get; init; } = default!;
    public string Provider { get; init; } = default!;
    public string? Model { get; init; }
    public string? ApiKeySecretName { get; init; }
    public string? Endpoint { get; init; }
}

public record AIProviderToolInvocation
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string ToolName { get; init; } = default!;
    public JsonObject Arguments { get; init; } = [];
}

public record AIProviderEvent
{
    public string Type { get; init; } = default!;
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public JsonObject Data { get; init; } = [];
}

public enum AIConversationStatus
{
    Active,
    Completed,
    Failed,
    Expired
}

public enum AIRetentionMode
{
    Configured,
    Ephemeral,
    Durable
}

public enum AIMessageRole
{
    User,
    Assistant,
    System,
    Tool
}
