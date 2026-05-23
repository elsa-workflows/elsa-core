using System.Collections.Concurrent;

namespace Elsa.AI.Host.Streaming;

public class AiStreamSessionManager
{
    private readonly ConcurrentDictionary<string, ReconnectState> _reconnectStates = new(StringComparer.OrdinalIgnoreCase);

    public void MarkDisconnected(string conversationId, TimeSpan graceWindow)
    {
        var now = DateTimeOffset.UtcNow;
        _reconnectStates[conversationId] = new ReconnectState(now.Add(graceWindow), false);
        PruneExpired(now);
    }

    public bool CanReconnect(string conversationId)
    {
        while (true)
        {
            var now = DateTimeOffset.UtcNow;
            PruneExpired(now);

            if (!_reconnectStates.TryGetValue(conversationId, out var state))
                return false;

            if (state.Deadline < now)
            {
                _reconnectStates.TryRemove(conversationId, out _);
                return false;
            }

            if (state.IsReserved)
                return false;

            if (_reconnectStates.TryUpdate(conversationId, state with { IsReserved = true }, state))
                return true;
        }
    }

    public void MarkConnected(string conversationId)
    {
        _reconnectStates.TryRemove(conversationId, out _);
    }

    public void ReleaseReconnect(string conversationId)
    {
        while (true)
        {
            var now = DateTimeOffset.UtcNow;
            PruneExpired(now);

            if (!_reconnectStates.TryGetValue(conversationId, out var state))
                return;

            if (state.Deadline < now)
            {
                _reconnectStates.TryRemove(conversationId, out _);
                return;
            }

            if (!state.IsReserved)
                return;

            if (_reconnectStates.TryUpdate(conversationId, state with { IsReserved = false }, state))
                return;
        }
    }

    private void PruneExpired(DateTimeOffset now)
    {
        foreach (var (conversationId, state) in _reconnectStates)
        {
            if (state.Deadline < now)
                _reconnectStates.TryRemove(conversationId, out _);
        }
    }

    private readonly record struct ReconnectState(DateTimeOffset Deadline, bool IsReserved);
}
