using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Providers.InMemory;

public class InMemoryStructuredLogStore : IStructuredLogStore
{
    private readonly RingBuffer<StructuredLogEvent> _recentLogs;
    private readonly IStructuredLogSourceRegistry _sourceRegistry;
    private readonly StructuredLogsOptions _options;

    public InMemoryStructuredLogStore(IOptions<StructuredLogsOptions> options, IStructuredLogSourceRegistry sourceRegistry)
    {
        _options = options.Value;
        _sourceRegistry = sourceRegistry;
        _recentLogs = new(_options.RecentLogCapacity);
    }

    public ValueTask WriteAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        _recentLogs.Add(logEvent);
        _sourceRegistry.MarkSeen(logEvent.SourceId, logEvent.ReceivedAt);
        return ValueTask.CompletedTask;
    }

    public ValueTask<RecentStructuredLogsResult> QueryAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(filter.Take ?? _options.MaxRecentLogQuerySize, 0, _options.MaxRecentLogQuerySize);
        var items = _recentLogs
            .Snapshot()
            .Where(x => StructuredLogFilterEvaluator.Matches(x, filter))
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.ReceivedAt)
            .ThenBy(x => x.SourceId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Sequence)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .TakeLast(take)
            .ToList();

        return ValueTask.FromResult(new RecentStructuredLogsResult(items, _recentLogs.DroppedCount));
    }

    public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_sourceRegistry.List());
    }
}
