using System.Runtime.CompilerServices;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Providers.InMemory;

public class InMemoryStructuredLogProvider : IStructuredLogStreamProvider
{
    private readonly InMemoryStructuredLogStore _store;
    private readonly InMemoryStructuredLogLiveFeed _liveFeed;

    public InMemoryStructuredLogProvider(IOptions<StructuredLogsOptions> options, IStructuredLogSourceRegistry sourceRegistry)
    {
        _store = new(options, sourceRegistry);
        _liveFeed = new(options);
    }

    public InMemoryStructuredLogProvider(InMemoryStructuredLogStore store, InMemoryStructuredLogLiveFeed liveFeed)
    {
        _store = store;
        _liveFeed = liveFeed;
    }

    public async ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        await _store.WriteAsync(logEvent, cancellationToken);
        await _liveFeed.PublishAsync(logEvent, cancellationToken);
    }

    public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
    {
        return _store.QueryAsync(filter, cancellationToken);
    }

    public async IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in SubscribeWithDroppedEventsAsync(filter, cancellationToken))
        {
            if (item.LogEvent != null)
                yield return item.LogEvent;
        }
    }

    public IAsyncEnumerable<StructuredLogStreamItem> SubscribeWithDroppedEventsAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
    {
        return _liveFeed.SubscribeAsync(filter, cancellationToken);
    }

    public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return _store.ListSourcesAsync(cancellationToken);
    }
}
