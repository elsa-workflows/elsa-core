using ConsoleLogStreaming.Core.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal sealed class ElsaConsoleLogRecentBuffer
{
    private readonly object _lock = new();
    private readonly Queue<ConsoleLogLine> _lines = new();
    private readonly int _capacity;
    private readonly int _maxQuerySize;

    public ElsaConsoleLogRecentBuffer(IOptions<ConsoleLogOptions> options)
    {
        _capacity = Math.Max(1, options.Value.RecentCapacity);
        _maxQuerySize = Math.Max(1, options.Value.MaxRecentQuerySize);
    }

    public void Add(ConsoleLogLine line)
    {
        lock (_lock)
        {
            _lines.Enqueue(line);

            while (_lines.Count > _capacity)
                _lines.Dequeue();
        }
    }

    public IReadOnlyList<ConsoleLogLine> Query(ConsoleLogFilter filter)
    {
        ConsoleLogLine[] snapshot;

        lock (_lock)
            snapshot = _lines.ToArray();

        var limit = filter.Limit is > 0 ? Math.Min(filter.Limit.Value, _maxQuerySize) : _maxQuerySize;
        return snapshot
            .Where(line => Matches(line, filter))
            .TakeLast(limit)
            .ToArray();
    }

    private static bool Matches(ConsoleLogLine line, ConsoleLogFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.SourceId) && !string.Equals(line.Source.Id, filter.SourceId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.Stream.HasValue && line.Stream != filter.Stream.Value)
            return false;

        if (!string.IsNullOrWhiteSpace(filter.Query) && !line.Text.Contains(filter.Query, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.From.HasValue && line.ReceivedAt < filter.From.Value)
            return false;

        if (filter.To.HasValue && line.ReceivedAt > filter.To.Value)
            return false;

        foreach (var (key, value) in filter.Metadata)
        {
            if (!line.Metadata.TryGetValue(key, out var candidate) || !string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
