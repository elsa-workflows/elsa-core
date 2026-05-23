using System.Collections.Concurrent;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Options;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Services;

public class InMemoryAiConversationStore(IOptions<AiHostOptions> options) : IAiTransientConversationStore
{
    private readonly ConcurrentDictionary<string, AiConversation> _conversations = new();

    public ValueTask<AiConversation?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        _conversations.TryGetValue(id, out var conversation);
        if (conversation == null || !IsExpired(conversation))
            return ValueTask.FromResult(conversation);

        _conversations.TryRemove(id, out _);
        conversation = null;

        return ValueTask.FromResult(conversation);
    }

    public ValueTask SaveAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        PruneExpired();
        _conversations[conversation.Id] = conversation;
        return ValueTask.CompletedTask;
    }

    private void PruneExpired()
    {
        foreach (var conversation in _conversations.Values.Where(IsExpired))
            _conversations.TryRemove(conversation.Id, out _);
    }

    private bool IsExpired(AiConversation conversation)
    {
        if (conversation.RetentionMode == AiRetentionMode.Ephemeral)
            return conversation.Status is AiConversationStatus.Completed or AiConversationStatus.Failed;

        if (conversation.RetentionMode == AiRetentionMode.Durable)
            return false;

        var retentionStartsAt = conversation.UpdatedAt != default ? conversation.UpdatedAt : conversation.CreatedAt;
        if (retentionStartsAt == default)
            return false;

        var expiresAt = conversation.RetentionExpiresAt ?? retentionStartsAt.Add(options.Value.ConversationRetention);
        return expiresAt <= DateTimeOffset.UtcNow;
    }
}
