using System.Collections.Concurrent;

namespace Elsa.AI.Host.Streaming;

public class AiStreamSessionManager
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _disconnectDeadlines = new(StringComparer.OrdinalIgnoreCase);

    public void MarkDisconnected(string conversationId, TimeSpan graceWindow)
    {
        var now = DateTimeOffset.UtcNow;
        _disconnectDeadlines[conversationId] = now.Add(graceWindow);
        PruneExpired(now);
    }

    public bool CanReconnect(string conversationId)
    {
        var now = DateTimeOffset.UtcNow;
        PruneExpired(now);

        if (!_disconnectDeadlines.TryRemove(conversationId, out var deadline))
            return false;

        if (deadline < now)
            return false;

        return true;
    }

    private void PruneExpired(DateTimeOffset now)
    {
        foreach (var (conversationId, deadline) in _disconnectDeadlines)
        {
            if (deadline < now)
                _disconnectDeadlines.TryRemove(conversationId, out _);
        }
    }
}
