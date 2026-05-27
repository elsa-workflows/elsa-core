using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.OpenTelemetry.Providers.InMemory;

public class InMemoryOpenTelemetryLiveFeed(IOptions<OpenTelemetryDiagnosticsOptions> options) : IOpenTelemetryLiveFeed
{
    private readonly OpenTelemetryDiagnosticsOptions _options = options.Value;
    private readonly object _subscribersLock = new();
    private readonly List<OpenTelemetrySubscriber> _subscribers = [];

    public ValueTask PublishAsync(OpenTelemetryBatch batch, CancellationToken cancellationToken = default)
    {
        List<OpenTelemetrySubscriber> subscribers;
        lock (_subscribersLock)
            subscribers = _subscribers.ToList();

        foreach (var subscriber in subscribers)
            subscriber.TryWrite(batch);

        return ValueTask.CompletedTask;
    }

    public async IAsyncEnumerable<OpenTelemetryStreamItem> SubscribeAsync(OpenTelemetryTraceFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriber = new OpenTelemetrySubscriber(filter, _options.SubscriberChannelCapacity);

        lock (_subscribersLock)
            _subscribers.Add(subscriber);

        try
        {
            await foreach (var item in subscriber.Channel.Reader.ReadAllAsync(cancellationToken))
            {
                subscriber.MarkRead(item);
                yield return item;
            }
        }
        finally
        {
            lock (_subscribersLock)
                _subscribers.Remove(subscriber);
        }
    }

    private sealed class OpenTelemetrySubscriber(OpenTelemetryTraceFilter filter, int channelCapacity)
    {
        private readonly object _lock = new();
        private readonly int _capacity = Math.Max(1, channelCapacity);
        private int _pendingItemCount;
        private readonly Dictionary<OpenTelemetrySignalType, long> _droppedSinceLastSummary = [];

        public Channel<OpenTelemetryStreamItem> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<OpenTelemetryStreamItem>(new BoundedChannelOptions(Math.Max(1, channelCapacity))
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        public void TryWrite(OpenTelemetryBatch batch)
        {
            foreach (var resource in batch.Resources)
                TryWrite(new OpenTelemetryStreamItem { Resource = resource });

            foreach (var trace in batch.Traces.Where(Matches))
                TryWrite(new OpenTelemetryStreamItem { Trace = trace });

            foreach (var log in batch.Logs.Where(x => string.IsNullOrWhiteSpace(filter.TraceId) || string.Equals(x.TraceId, filter.TraceId, StringComparison.OrdinalIgnoreCase)))
                TryWrite(new OpenTelemetryStreamItem { Log = log });

            foreach (var point in batch.MetricPoints)
                TryWrite(new OpenTelemetryStreamItem { MetricPoint = point });
        }

        private void TryWrite(OpenTelemetryStreamItem item)
        {
            lock (_lock)
            {
                EnsureCapacityForWrite();
                if (Channel.Writer.TryWrite(item))
                    _pendingItemCount++;
                else
                    TrackDrop(item);

                TryWriteDroppedSummary();
            }
        }

        public void MarkRead(OpenTelemetryStreamItem item)
        {
            lock (_lock)
            {
                _pendingItemCount = Math.Max(0, _pendingItemCount - 1);

                if (item.DroppedItems != null)
                    _droppedSinceLastSummary.Remove(item.DroppedItems.SignalType);

                TryWriteDroppedSummary();
            }
        }

        private void EnsureCapacityForWrite()
        {
            if (_pendingItemCount < _capacity)
                return;

            if (!Channel.Reader.TryRead(out var dropped))
                return;

            _pendingItemCount = Math.Max(0, _pendingItemCount - 1);
            TrackDrop(dropped);
        }

        private void TrackDrop(OpenTelemetryStreamItem item)
        {
            var signalType = GetSignalType(item);
            if (signalType == null)
                return;

            _droppedSinceLastSummary.TryGetValue(signalType.Value, out var count);
            _droppedSinceLastSummary[signalType.Value] = count + (item.DroppedItems?.Count ?? 1);
        }

        private void TryWriteDroppedSummary()
        {
            if (_droppedSinceLastSummary.Count == 0)
                return;

            if (_pendingItemCount >= _capacity)
                return;

            var dropped = _droppedSinceLastSummary.OrderBy(x => x.Key).First();
            if (Channel.Writer.TryWrite(new OpenTelemetryStreamItem { DroppedItems = new(dropped.Key, dropped.Value, "SubscriberQueueFull") }))
                _pendingItemCount++;
        }

        private static OpenTelemetrySignalType? GetSignalType(OpenTelemetryStreamItem item)
        {
            if (item.Trace != null)
                return OpenTelemetrySignalType.Trace;

            if (item.MetricPoint != null)
                return OpenTelemetrySignalType.Metric;

            if (item.Log != null)
                return OpenTelemetrySignalType.Log;

            return item.DroppedItems?.SignalType;
        }

        private bool Matches(TelemetryTrace trace)
        {
            if (!string.IsNullOrWhiteSpace(filter.TraceId) && !string.Equals(trace.TraceId, filter.TraceId, StringComparison.OrdinalIgnoreCase))
                return false;

            if (filter.Status != null && trace.Status != filter.Status)
                return false;

            return true;
        }
    }
}
