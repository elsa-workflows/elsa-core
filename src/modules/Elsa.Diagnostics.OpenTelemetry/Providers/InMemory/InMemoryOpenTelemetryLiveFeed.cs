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
            subscriber.TryWrite(batch, _options.SubscriberChannelCapacity);

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
                subscriber.MarkRead();
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
        private int _pendingItemCount;
        private long _droppedSinceLastSummary;

        public Channel<OpenTelemetryStreamItem> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<OpenTelemetryStreamItem>(new BoundedChannelOptions(Math.Max(1, channelCapacity))
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        public void TryWrite(OpenTelemetryBatch batch, int capacity)
        {
            foreach (var resource in batch.Resources)
                TryWrite(new OpenTelemetryStreamItem { Resource = resource }, capacity);

            foreach (var trace in batch.Traces.Where(Matches))
                TryWrite(new OpenTelemetryStreamItem { Trace = trace }, capacity);

            foreach (var log in batch.Logs.Where(x => string.IsNullOrWhiteSpace(filter.TraceId) || string.Equals(x.TraceId, filter.TraceId, StringComparison.OrdinalIgnoreCase)))
                TryWrite(new OpenTelemetryStreamItem { Log = log }, capacity);

            foreach (var point in batch.MetricPoints)
                TryWrite(new OpenTelemetryStreamItem { MetricPoint = point }, capacity);
        }

        private void TryWrite(OpenTelemetryStreamItem item, int capacity)
        {
            lock (_lock)
            {
                if (_pendingItemCount >= capacity)
                {
                    _droppedSinceLastSummary++;
                    Channel.Writer.TryWrite(item);
                    Channel.Writer.TryWrite(new OpenTelemetryStreamItem { DroppedItems = new(OpenTelemetrySignalType.Trace, _droppedSinceLastSummary, "SubscriberQueueFull") });
                    _droppedSinceLastSummary = 0;
                    return;
                }

                if (_droppedSinceLastSummary > 0)
                {
                    Channel.Writer.TryWrite(new OpenTelemetryStreamItem { DroppedItems = new(OpenTelemetrySignalType.Trace, _droppedSinceLastSummary, "SubscriberQueueFull") });
                    _pendingItemCount++;
                    _droppedSinceLastSummary = 0;
                }

                Channel.Writer.TryWrite(item);
                _pendingItemCount++;
            }
        }

        public void MarkRead()
        {
            lock (_lock)
                _pendingItemCount = Math.Max(0, _pendingItemCount - 1);
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
