using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Elsa.ServerLogs.Contracts;
using Elsa.ServerLogs.Models;
using Elsa.ServerLogs.Options;
using Elsa.ServerLogs.Services;
using Microsoft.Extensions.Options;

namespace Elsa.ServerLogs.Providers.InMemory;

public class InMemoryServerLogProvider : IServerLogStreamProvider
{
    private readonly RingBuffer<ServerLogEvent> _recentLogs;
    private readonly IServerLogSourceRegistry _sourceRegistry;
    private readonly ServerLogStreamingOptions _options;
    private readonly object _subscribersLock = new();
    private readonly Dictionary<Guid, ServerLogSubscriber> _subscribers = new();
    
    public InMemoryServerLogProvider(IOptions<ServerLogStreamingOptions> options, IServerLogSourceRegistry sourceRegistry)
    {
        _options = options.Value;
        _sourceRegistry = sourceRegistry;
        _recentLogs = new(_options.RecentLogCapacity);
    }
    
    public ValueTask PublishAsync(ServerLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        _recentLogs.Add(logEvent);
        _sourceRegistry.MarkSeen(logEvent.SourceId, logEvent.ReceivedAt);
        
        List<ServerLogSubscriber> subscribers;
        lock (_subscribersLock)
            subscribers = _subscribers.Values.ToList();

        foreach (var subscriber in subscribers)
            subscriber.TryWrite(logEvent, _options.SubscriberChannelCapacity);
        
        return ValueTask.CompletedTask;
    }
    
    public ValueTask<RecentServerLogsResult> GetRecentAsync(ServerLogFilter filter, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(filter.Take ?? _options.MaxRecentLogQuerySize, 0, _options.MaxRecentLogQuerySize);
        var items = _recentLogs
            .Snapshot()
            .Where(x => ServerLogFilterEvaluator.Matches(x, filter))
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.ReceivedAt)
            .ThenBy(x => x.SourceId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Sequence)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .TakeLast(take)
            .ToList();
        
        return ValueTask.FromResult(new RecentServerLogsResult(items, _recentLogs.DroppedCount));
    }
    
    public async IAsyncEnumerable<ServerLogEvent> SubscribeAsync(ServerLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in SubscribeWithDroppedEventsAsync(filter, cancellationToken))
        {
            if (item.LogEvent != null)
                yield return item.LogEvent;
        }
    }

    public async IAsyncEnumerable<ServerLogStreamItem> SubscribeWithDroppedEventsAsync(ServerLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriberId = Guid.NewGuid();
        var subscriber = new ServerLogSubscriber(filter);

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
    
    public ValueTask<IReadOnlyCollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_sourceRegistry.List());
    }

    private sealed class ServerLogSubscriber(ServerLogFilter filter)
    {
        private readonly object _lock = new();
        private int _pendingItemCount;
        private long _droppedSinceLastSummary;
        private bool _summaryQueued;

        public Channel<ServerLogStreamItem> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<ServerLogStreamItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        public void TryWrite(ServerLogEvent logEvent, int capacity)
        {
            if (!ServerLogFilterEvaluator.Matches(logEvent, filter))
                return;

            lock (_lock)
            {
                if (_pendingItemCount >= capacity)
                {
                    _droppedSinceLastSummary++;
                    QueueDroppedSummaryIfNeeded();
                    return;
                }

                Channel.Writer.TryWrite(ServerLogStreamItem.FromLogEvent(logEvent));
                _pendingItemCount++;
            }
        }

        public void MarkConsumed(ServerLogStreamItem item)
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

            var summary = new ServerLogDroppedEventSummary(null, _droppedSinceLastSummary, "SubscriberChannelFull");
            _droppedSinceLastSummary = 0;
            _summaryQueued = true;
            _pendingItemCount++;
            Channel.Writer.TryWrite(ServerLogStreamItem.FromDroppedEvents(summary));
        }
    }
}
