using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Providers.InMemory;

public class InMemoryStructuredLogLiveFeed(IOptions<StructuredLogsOptions> options) : IStructuredLogLiveFeed
{
    private readonly StructuredLogsOptions _options = options.Value;
    private readonly object _subscribersLock = new();
    private readonly Dictionary<Guid, StructuredLogSubscriber> _subscribers = new();

    public ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        List<StructuredLogSubscriber> subscribers;
        lock (_subscribersLock)
            subscribers = _subscribers.Values.ToList();

        foreach (var subscriber in subscribers)
            subscriber.TryWrite(logEvent, _options.SubscriberChannelCapacity);

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<StructuredLogStreamItem> SubscribeAsync(StructuredLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
