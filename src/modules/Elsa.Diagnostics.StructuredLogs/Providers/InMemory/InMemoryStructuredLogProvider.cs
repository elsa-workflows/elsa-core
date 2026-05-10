using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Providers.InMemory;

public class InMemoryStructuredLogProvider : IStructuredLogStreamProvider
{
    private readonly RingBuffer<StructuredLogEvent> _recentLogs;
    private readonly IStructuredLogSourceRegistry _sourceRegistry;
    private readonly StructuredLogsOptions _options;
    private readonly object _subscribersLock = new();
    private readonly Dictionary<Guid, StructuredLogSubscriber> _subscribers = new();
    
    public InMemoryStructuredLogProvider(IOptions<StructuredLogsOptions> options, IStructuredLogSourceRegistry sourceRegistry)
    {
        _options = options.Value;
        _sourceRegistry = sourceRegistry;
        _recentLogs = new(_options.RecentLogCapacity);
    }
    
    public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        _recentLogs.Add(logEvent);
        _sourceRegistry.MarkSeen(logEvent.SourceId, logEvent.ReceivedAt);
        
        List<StructuredLogSubscriber> subscribers;
        lock (_subscribersLock)
            subscribers = _subscribers.Values.ToList();

        foreach (var subscriber in subscribers)
            subscriber.TryWrite(logEvent, _options.SubscriberChannelCapacity);
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)
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
    
    public async IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in SubscribeWithDroppedEventsAsync(filter, cancellationToken))
        {
            if (item.LogEvent != null)
                yield return item.LogEvent;
        }
    }

    public async IAsyncEnumerable<StructuredLogStreamItem> SubscribeWithDroppedEventsAsync(StructuredLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriberId = Guid.NewGuid();
        var subscriber = new StructuredLogSubscriber(filter);

        lock (_subscribersLock)
            _subscribers[subscriberId] = subscriber;

        try
        {
            await foreach (var item in subscriber.Channel.Reader.ReadAllAsync(cancellationToken))
            {
                subscriber.MarkConsumed(item);
                yield return item;
            }
        }
        finally
        {
            lock (_subscribersLock)
                _subscribers.Remove(subscriberId);
        }
    }
    
    public ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_sourceRegistry.List());
    }

    private sealed class StructuredLogSubscriber(StructuredLogFilter filter)
    {
        private readonly object _lock = new();
        private int _pendingItemCount;
        private long _droppedSinceLastSummary;
        private bool _summaryQueued;

        public Channel<StructuredLogStreamItem> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<StructuredLogStreamItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        public void TryWrite(StructuredLogEvent logEvent, int capacity)
        {
            if (!StructuredLogFilterEvaluator.Matches(logEvent, filter))
                return;

            lock (_lock)
            {
                if (_pendingItemCount >= capacity)
                {
                    _droppedSinceLastSummary++;
                    QueueDroppedSummaryIfNeeded();
                    return;
                }

                Channel.Writer.TryWrite(StructuredLogStreamItem.FromLogEvent(logEvent));
                _pendingItemCount++;
            }
        }

        public void MarkConsumed(StructuredLogStreamItem item)
        {
            lock (_lock)
            {
                _pendingItemCount = Math.Max(0, _pendingItemCount - 1);

                if (item.DroppedEvents != null)
                {
                    _summaryQueued = false;
                    QueueDroppedSummaryIfNeeded();
                }
            }
        }

        private void QueueDroppedSummaryIfNeeded()
        {
            if (_summaryQueued || _droppedSinceLastSummary == 0)
                return;

            var summary = new StructuredLogDroppedEventSummary(null, _droppedSinceLastSummary, "SubscriberChannelFull");
            _droppedSinceLastSummary = 0;
            _summaryQueued = true;
            _pendingItemCount++;
            Channel.Writer.TryWrite(StructuredLogStreamItem.FromDroppedEvents(summary));
        }
    }
}
