using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.Stores;

public class EFCoreAiConversationStore(AiDbContext dbContext) : IAiConversationStore
{
    private const int MaxStoredMessages = 256;
    private const int MaxMessagesJsonBytes = 1024 * 1024;

    public async ValueTask<AiConversation?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Conversations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record == null)
            return null;

        var conversation = Map(record);
        if (!IsExpired(conversation))
            return conversation;

        try
        {
            dbContext.Conversations.Remove(record);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Expired-record cleanup is best-effort; stale cleanup should not block starting a fresh conversation.
        }

        return null;
    }

    public async ValueTask SaveAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        Validate(conversation);
        var isNew = false;
        var record = await dbContext.Conversations.FindAsync([conversation.Id], cancellationToken);
        if (record == null)
        {
            record = new AiConversationRecord { Id = conversation.Id };
            dbContext.Conversations.Add(record);
            isNew = true;
        }
        else if (!string.Equals(record.TenantId, conversation.TenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot overwrite an AI conversation that belongs to another tenant.");
        }

        Map(conversation, record);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (isNew)
        {
            await RetryAsUpdateAsync(conversation, cancellationToken);
        }
    }

    private async ValueTask RetryAsUpdateAsync(AiConversation conversation, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var record = await dbContext.Conversations.FindAsync([conversation.Id], cancellationToken);
        if (record == null)
            throw new DbUpdateException($"Failed to insert AI conversation {conversation.Id}, and no existing record was found for retry.");

        if (!string.Equals(record.TenantId, conversation.TenantId, StringComparison.Ordinal))
            throw new InvalidOperationException("Cannot overwrite an AI conversation that belongs to another tenant.");

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
        record.Messages = SerializeMessages(conversation.Messages);
    }

    private static string SerializeMessages(IReadOnlyCollection<AiMessage> messages)
    {
        var boundedMessages = messages.Count > MaxStoredMessages
            ? messages.Skip(messages.Count - MaxStoredMessages).ToList()
            : messages.ToList();
        var json = JsonSerializer.Serialize(boundedMessages);

        while (boundedMessages.Count > 1 && Encoding.UTF8.GetByteCount(json) > MaxMessagesJsonBytes)
        {
            boundedMessages.RemoveAt(0);
            json = JsonSerializer.Serialize(boundedMessages);
        }

        if (boundedMessages.Count == 1 && Encoding.UTF8.GetByteCount(json) > MaxMessagesJsonBytes)
        {
            var message = boundedMessages[0];
            boundedMessages[0] = message with
            {
                Content = Truncate(message.Content, MaxMessagesJsonBytes / 4),
                Metadata = new JsonObject
                {
                    ["truncated"] = true,
                    ["maxBytes"] = MaxMessagesJsonBytes
                }
            };
            json = JsonSerializer.Serialize(boundedMessages);
        }

        return json;
    }

    private static string Truncate(string value, int maxCharacters) =>
        value.Length <= maxCharacters ? value : value[..maxCharacters];

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
