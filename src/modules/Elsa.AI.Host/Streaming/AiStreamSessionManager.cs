using System.Collections.Concurrent;

namespace Elsa.AI.Host.Streaming;

public class AiStreamSessionManager
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _disconnectDeadlines = new(StringComparer.OrdinalIgnoreCase);

    public void MarkDisconnected(string conversationId, TimeSpan graceWindow)
    {
        _disconnectDeadlines[conversationId] = DateTimeOffset.UtcNow.Add(graceWindow);
    }

    public bool CanReconnect(string conversationId)
    {
        return _disconnectDeadlines.TryGetValue(conversationId, out var deadline) && deadline >= DateTimeOffset.UtcNow;
    }
}
