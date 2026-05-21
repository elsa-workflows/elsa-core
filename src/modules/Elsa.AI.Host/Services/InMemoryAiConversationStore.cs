using System.Collections.Concurrent;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Services;

public class InMemoryAiConversationStore : IAiConversationStore
{
    private readonly ConcurrentDictionary<string, AiConversation> _conversations = new();

    public ValueTask<AiConversation?> FindAsync(string id, CancellationToken cancellationToken = default)
    {
        _conversations.TryGetValue(id, out var conversation);
        return ValueTask.FromResult(conversation);
    }

    public ValueTask SaveAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        _conversations[conversation.Id] = conversation;
        return ValueTask.CompletedTask;
    }
}
