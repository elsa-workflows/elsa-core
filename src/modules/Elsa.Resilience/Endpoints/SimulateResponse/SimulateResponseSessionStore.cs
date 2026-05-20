using Elsa.Resilience.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Resilience.Endpoints.SimulateResponse;

public class SimulateResponseSessionStore(IOptions<SimulateResponseOptions> options, TimeProvider timeProvider)
{
    private readonly Dictionary<string, SessionState> _sessions = new(StringComparer.Ordinal);
    private readonly object _lock = new();
    private readonly SimulateResponseOptions _options = options.Value;

    public bool TryGetNextIndex(string sessionId, int statusCodeCount, out int nextIndex)
    {
        if (statusCodeCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(statusCodeCount), "Status code count must be greater than zero.");

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(_options.SessionSlidingExpiration);

        lock (_lock)
        {
            PruneExpired(now);

            nextIndex = _sessions.TryGetValue(sessionId, out var state) ? Math.Min(state.NextIndex, statusCodeCount - 1) : 0;

            if (nextIndex + 1 >= statusCodeCount)
            {
                _sessions.Remove(sessionId);
                return true;
            }

            if (!_sessions.ContainsKey(sessionId) && _sessions.Count >= _options.SessionCapacity)
                return false;

            _sessions[sessionId] = new SessionState(nextIndex + 1, expiresAt);
            return true;
        }
    }

    private void PruneExpired(DateTimeOffset now)
    {
        foreach (var session in _sessions.Where(x => x.Value.ExpiresAt <= now).Select(x => x.Key).ToList())
            _sessions.Remove(session);
    }

    private sealed record SessionState(int NextIndex, DateTimeOffset ExpiresAt);
}
