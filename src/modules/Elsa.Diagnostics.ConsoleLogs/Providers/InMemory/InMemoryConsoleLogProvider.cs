using System.Threading.Channels;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Providers.InMemory;

public class InMemoryConsoleLogProvider(IOptions<ConsoleLogsOptions> options, IConsoleLogSourceRegistry sourceRegistry) : IConsoleLogProvider
{
    private readonly ConsoleLogsOptions _options = options.Value;
    private readonly RingBuffer<ConsoleLogLine> _recentLines = new(options.Value.RecentLogCapacity);
    private readonly object _subscribersLock = new();
    private readonly Dictionary<Guid, ConsoleLogSubscriber> _subscribers = new();

    public ValueTask PublishAsync(ConsoleLogLine line, CancellationToken cancellationToken = default)
    {
        _recentLines.Add(line);
        sourceRegistry.MarkSeen(line.Source.Id, line.ReceivedAt);

        List<ConsoleLogSubscriber> subscribers;
        lock (_subscribersLock)
            subscribers = _subscribers.Values.ToList();

        foreach (var subscriber in subscribers)
            subscriber.TryWrite(line, _options.SubscriberChannelCapacity);

        return ValueTask.CompletedTask;
    }

    public ValueTask<RecentConsoleLogsResult> GetRecentAsync(ConsoleLogFilter filter, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(filter.Limit ?? _options.MaxRecentQuerySize, 0, _options.MaxRecentQuerySize);
        var items = _recentLines
            .Snapshot()
            .Where(x => ConsoleLogFilterEvaluator.Matches(x, filter))
            .OrderBy(x => x.ReceivedAt)
            .ThenBy(x => x.Timestamp)
            .ThenBy(x => x.Source.Id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Sequence)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .TakeLast(limit)
            .ToList();

        IReadOnlyCollection<ConsoleLogDroppedSummary> dropped = _recentLines.DroppedCount == 0
            ? []
            : [new ConsoleLogDroppedSummary(null, null, "RecentBufferFull", _recentLines.DroppedCount)];

        return ValueTask.FromResult(new RecentConsoleLogsResult(items, dropped));
    }

    public async IAsyncEnumerable<ConsoleLogStreamItem> SubscribeAsync(ConsoleLogFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriberId = Guid.NewGuid();
        var subscriber = new ConsoleLogSubscriber(filter);

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

    public ValueTask<IReadOnlyCollection<ConsoleLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(sourceRegistry.List());
    }

    private sealed class ConsoleLogSubscriber(ConsoleLogFilter filter)
    {
        private readonly object _lock = new();
        private int _pendingItemCount;
        private long _droppedSinceLastSummary;
        private bool _summaryQueued;

        public Channel<ConsoleLogStreamItem> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<ConsoleLogStreamItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        public void TryWrite(ConsoleLogLine line, int capacity)
        {
            if (!ConsoleLogFilterEvaluator.Matches(line, filter))
                return;

            lock (_lock)
            {
                if (_pendingItemCount >= capacity)
                {
                    _droppedSinceLastSummary++;
                    QueueDroppedSummaryIfNeeded(line);
                    return;
                }

                Channel.Writer.TryWrite(ConsoleLogStreamItem.FromLine(line));
                _pendingItemCount++;
            }
        }

        public void MarkConsumed(ConsoleLogStreamItem item)
        {
            lock (_lock)
            {
                _pendingItemCount = Math.Max(0, _pendingItemCount - 1);

                if (item.DroppedLines != null)
                {
                    _summaryQueued = false;
                    QueueDroppedSummaryIfNeeded(item.Line);
                }
            }
        }

        private void QueueDroppedSummaryIfNeeded(ConsoleLogLine? line)
        {
            if (_summaryQueued || _droppedSinceLastSummary == 0)
                return;

            var summary = new ConsoleLogDroppedSummary(line?.Source.Id, line?.Stream, "SubscriberChannelFull", _droppedSinceLastSummary);
            _droppedSinceLastSummary = 0;
            _summaryQueued = true;
            _pendingItemCount++;
            Channel.Writer.TryWrite(ConsoleLogStreamItem.FromDroppedLines(summary));
        }
    }
}
