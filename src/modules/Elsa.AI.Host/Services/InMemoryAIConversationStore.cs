using System.Collections.Concurrent;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Services;

public class InMemoryAIConversationStore : IAITransientConversationStore
{
    private readonly ConcurrentDictionary<string, AIConversation> _conversations = new();

    public ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        _conversations.TryGetValue(id, out var conversation);
        if (conversation == null || !IsExpired(conversation))
            return ValueTask.FromResult(conversation);

        _conversations.TryRemove(id, out _);
        conversation = null;

        return ValueTask.FromResult(conversation);
    }

    public ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default)
    {
        PruneExpired();
        _conversations.AddOrUpdate(
            conversation.Id,
            conversation,
            (_, existing) =>
            {
                ValidateOwnership(existing, conversation);
                return conversation;
            });

        return ValueTask.CompletedTask;
    }

    private void PruneExpired()
    {
        foreach (var conversation in _conversations.Values.Where(IsExpired))
            _conversations.TryRemove(conversation.Id, out _);
    }

    private bool IsExpired(AIConversation conversation)
    {
        if (conversation.RetentionMode == AIRetentionMode.Ephemeral)
            return conversation.Status is AIConversationStatus.Completed or AIConversationStatus.Failed;

        if (conversation.RetentionMode == AIRetentionMode.Durable)
            return false;

        var expiresAt = conversation.RetentionExpiresAt;
        return expiresAt.HasValue && expiresAt <= DateTimeOffset.UtcNow;
    }

    private static void ValidateOwnership(AIConversation existing, AIConversation conversation)
    {
        if (!string.Equals(NormalizeTenantId(existing.TenantId), NormalizeTenantId(conversation.TenantId), StringComparison.Ordinal))
            throw new InvalidOperationException("Cannot overwrite an AI conversation that belongs to another tenant.");

        if (!string.IsNullOrWhiteSpace(existing.UserId) && !string.Equals(existing.UserId, conversation.UserId, StringComparison.Ordinal))
            throw new InvalidOperationException("Cannot overwrite an AI conversation that belongs to another user.");
    }

    private static string NormalizeTenantId(string? tenantId) => tenantId ?? "";
}
