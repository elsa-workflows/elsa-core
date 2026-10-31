using System.Runtime.CompilerServices;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Services;

public class DefaultStructuredLogProvider(IStructuredLogStore store, IStructuredLogLiveFeed liveFeed) : IStructuredLogStreamProvider
{
    public async ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        await store.WriteAsync(logEvent, cancellationToken);
        await liveFeed.PublishAsync(logEvent, cancellationToken);
    }

    public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
    {
        return store.QueryAsync(filter, cancellationToken);
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
        return liveFeed.SubscribeAsync(filter, cancellationToken);
    }

    public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return store.ListSourcesAsync(cancellationToken);
    }
}
