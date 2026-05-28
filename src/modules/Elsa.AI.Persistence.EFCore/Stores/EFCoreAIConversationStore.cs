using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.Stores;

public class EFCoreAIConversationStore(AIDbContext dbContext) : IAIConversationStore
{
    private const int MaxStoredMessages = 256;
    private const int MaxMessagesJsonBytes = 1024 * 1024;

    public async ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.Conversations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (record == null)
            return null;

        var conversation = Map(record);
        if (!IsExpired(conversation))
            return conversation;

        try
        {
            await dbContext.Conversations
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Expired-record cleanup is best-effort; stale cleanup should not block starting a fresh conversation.
        }

        return null;
    }

    public async ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default)
    {
        Validate(conversation);
        var isNew = false;
        var record = await dbContext.Conversations.FindAsync([conversation.Id], cancellationToken);
        if (record == null)
        {
            record = new AIConversationRecord { Id = conversation.Id };
            dbContext.Conversations.Add(record);
            isNew = true;
        }
        else if (!BelongsToTenant(record.TenantId, conversation.TenantId))
        {
            throw new InvalidOperationException("Cannot overwrite an AI conversation that belongs to another tenant.");
        }
        else
        {
            ValidateUserOwnership(record, conversation);
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

    private async ValueTask RetryAsUpdateAsync(AIConversation conversation, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        var record = await dbContext.Conversations.FindAsync([conversation.Id], cancellationToken);
        if (record == null)
            throw new DbUpdateException($"Failed to insert AI conversation {conversation.Id}, and no existing record was found for retry.");

        if (!BelongsToTenant(record.TenantId, conversation.TenantId))
            throw new InvalidOperationException("Cannot overwrite an AI conversation that belongs to another tenant.");

        ValidateUserOwnership(record, conversation);

        Map(conversation, record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static AIConversation Map(AIConversationRecord record) =>
        new()
        {
            Id = record.Id,
            TenantId = record.TenantId,
            UserId = record.UserId,
            Title = record.Title,
            Status = ParseEnum(record.Status, AIConversationStatus.Active),
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            ProviderSessionId = record.ProviderSessionId,
            RetentionMode = ParseEnum(record.RetentionMode, AIRetentionMode.Configured),
            RetentionExpiresAt = record.RetentionExpiresAt,
            Messages = JsonSerializer.Deserialize<IReadOnlyCollection<AIMessage>>(record.Messages) ?? []
        };

    private static void Map(AIConversation conversation, AIConversationRecord record)
    {
        record.TenantId = NormalizeTenantId(conversation.TenantId);
        record.UserId = conversation.UserId;
        record.Title = conversation.Title;
        record.Status = conversation.Status.ToString();
        if (record.CreatedAt == default)
            record.CreatedAt = conversation.CreatedAt;

        record.UpdatedAt = conversation.UpdatedAt;
        record.ProviderSessionId = conversation.ProviderSessionId;
        record.RetentionMode = conversation.RetentionMode.ToString();
        if (record.RetentionExpiresAt == null)
            record.RetentionExpiresAt = conversation.RetentionExpiresAt;

        record.Messages = SerializeMessages(conversation.Messages);
    }

    private static string SerializeMessages(IReadOnlyCollection<AIMessage> messages)
    {
        var orderedMessages = messages
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.StreamSequence)
            .ToList();
        var boundedMessages = orderedMessages.Count > MaxStoredMessages
            ? orderedMessages.Skip(orderedMessages.Count - MaxStoredMessages).ToList()
            : orderedMessages;
        var json = JsonSerializer.Serialize(boundedMessages);

        if (boundedMessages.Count > 1 && Encoding.UTF8.GetByteCount(json) > MaxMessagesJsonBytes)
            (boundedMessages, json) = ShrinkMessagesToByteLimit(boundedMessages);

        if (boundedMessages.Count == 1 && Encoding.UTF8.GetByteCount(json) > MaxMessagesJsonBytes)
            json = SerializeSingleTruncatedMessage(boundedMessages[0]);

        return json;
    }

    private static string SerializeSingleTruncatedMessage(AIMessage message)
    {
        var candidateLength = Math.Min(message.Content.Length, MaxMessagesJsonBytes / 4);

        while (true)
        {
            candidateLength = NormalizeSliceLength(message.Content, candidateLength);
            var candidateContent = message.Content[..candidateLength];
            var candidateJson = JsonSerializer.Serialize(new[] { CreateTruncatedMessage(message, candidateContent) });

            if (candidateLength == 0 || Encoding.UTF8.GetByteCount(candidateJson) <= MaxMessagesJsonBytes)
                return candidateJson;

            candidateLength -= Math.Max(1, candidateLength / 10);
        }
    }

    private static AIMessage CreateTruncatedMessage(AIMessage message, string content) =>
        message with
        {
            Content = content,
            Metadata = new JsonObject
            {
                ["truncated"] = true,
                ["maxBytes"] = MaxMessagesJsonBytes
            }
        };

    private static int NormalizeSliceLength(string value, int length)
    {
        if (length > 0 && char.IsHighSurrogate(value[length - 1]))
            length--;

        return length;
    }

    private static (List<AIMessage> Messages, string Json) ShrinkMessagesToByteLimit(List<AIMessage> messages)
    {
        var low = 1;
        var high = messages.Count;
        var bestMessages = messages.Skip(messages.Count - 1).ToList();
        var bestJson = JsonSerializer.Serialize(bestMessages);

        while (low < high)
        {
            var candidateCount = (low + high + 1) / 2;
            var candidateMessages = messages.Skip(messages.Count - candidateCount).ToList();
            var candidateJson = JsonSerializer.Serialize(candidateMessages);

            if (Encoding.UTF8.GetByteCount(candidateJson) <= MaxMessagesJsonBytes)
            {
                low = candidateCount;
                bestMessages = candidateMessages;
                bestJson = candidateJson;
            }
            else
            {
                high = candidateCount - 1;
            }
        }

        return (bestMessages, bestJson);
    }

    private static bool IsExpired(AIConversation conversation)
    {
        if (conversation.RetentionMode == AIRetentionMode.Ephemeral)
            return conversation.Status is AIConversationStatus.Completed or AIConversationStatus.Failed;

        if (conversation.RetentionMode == AIRetentionMode.Durable)
            return false;

        var expiresAt = conversation.RetentionExpiresAt;
        if (expiresAt == null)
            return false;

        return expiresAt <= DateTimeOffset.UtcNow;
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : defaultValue;

    private static bool BelongsToTenant(string? storedTenantId, string? requestedTenantId) =>
        string.Equals(NormalizeTenantId(storedTenantId), NormalizeTenantId(requestedTenantId), StringComparison.Ordinal);

    private static string NormalizeTenantId(string? tenantId) => tenantId ?? "";

    private static void ValidateUserOwnership(AIConversationRecord record, AIConversation conversation)
    {
        if (!string.IsNullOrWhiteSpace(record.UserId) && !string.Equals(record.UserId, conversation.UserId, StringComparison.Ordinal))
            throw new InvalidOperationException("Cannot overwrite an AI conversation that belongs to another user.");
    }

    private static void Validate(AIConversation conversation)
    {
        if (string.IsNullOrWhiteSpace(conversation.Id))
            throw new ArgumentException("A conversation ID is required.", nameof(conversation));

        if (string.IsNullOrWhiteSpace(conversation.UserId))
            throw new ArgumentException("A conversation user ID is required.", nameof(conversation));
    }
}
