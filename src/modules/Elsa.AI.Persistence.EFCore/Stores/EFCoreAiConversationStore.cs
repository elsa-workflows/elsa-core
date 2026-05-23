using System.Text.Json;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.Stores;

public class EFCoreAiConversationStore(AiDbContext dbContext) : IAiConversationStore
{
    public async ValueTask<AiConversation?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Conversations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record == null)
            return null;

        var conversation = Map(record);
        if (!IsExpired(conversation))
            return conversation;

        dbContext.Conversations.Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async ValueTask SaveAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        Validate(conversation);
        var record = await dbContext.Conversations.FindAsync([conversation.Id], cancellationToken);
        if (record == null)
        {
            record = new AiConversationRecord { Id = conversation.Id };
            dbContext.Conversations.Add(record);
        }

        Map(conversation, record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AiConversation Map(AiConversationRecord record) =>
        new()
        {
            Id = record.Id,
            TenantId = record.TenantId,
            UserId = record.UserId,
            Title = record.Title,
            Status = ParseEnum(record.Status, AiConversationStatus.Active),
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            ProviderSessionId = record.ProviderSessionId,
            RetentionMode = ParseEnum(record.RetentionMode, AiRetentionMode.Configured),
            RetentionExpiresAt = record.RetentionExpiresAt,
            Messages = JsonSerializer.Deserialize<IReadOnlyCollection<AiMessage>>(record.Messages) ?? []
        };

    private static void Map(AiConversation conversation, AiConversationRecord record)
    {
        record.TenantId = conversation.TenantId;
        record.UserId = conversation.UserId;
        record.Title = conversation.Title;
        record.Status = conversation.Status.ToString();
        record.CreatedAt = conversation.CreatedAt;
        record.UpdatedAt = conversation.UpdatedAt;
        record.ProviderSessionId = conversation.ProviderSessionId;
        record.RetentionMode = conversation.RetentionMode.ToString();
        record.RetentionExpiresAt = conversation.RetentionExpiresAt;
        record.Messages = JsonSerializer.Serialize(conversation.Messages);
    }

    private static bool IsExpired(AiConversation conversation)
    {
        if (conversation.RetentionMode == AiRetentionMode.Ephemeral)
            return conversation.Status is AiConversationStatus.Completed or AiConversationStatus.Failed;

        if (conversation.RetentionMode == AiRetentionMode.Durable)
            return false;

        var expiresAt = conversation.RetentionExpiresAt;
        if (expiresAt == null)
            return false;

        return expiresAt <= DateTimeOffset.UtcNow;
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : defaultValue;

    private static void Validate(AiConversation conversation)
    {
        if (string.IsNullOrWhiteSpace(conversation.Id))
            throw new ArgumentException("A conversation ID is required.", nameof(conversation));

        if (string.IsNullOrWhiteSpace(conversation.UserId))
            throw new ArgumentException("A conversation user ID is required.", nameof(conversation));
    }
}
