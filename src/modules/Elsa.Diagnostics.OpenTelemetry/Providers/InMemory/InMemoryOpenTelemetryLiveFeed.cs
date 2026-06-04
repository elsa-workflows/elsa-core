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
            var serviceResourceIds = ResolveServiceResourceIds(batch.Resources);

            foreach (var resource in batch.Resources.Where(MatchesResource))
                TryWrite(new OpenTelemetryStreamItem { Resource = resource });

            foreach (var trace in batch.Traces.Where(trace => MatchesTrace(trace, serviceResourceIds)))
                TryWrite(new OpenTelemetryStreamItem { Trace = trace });

            foreach (var log in batch.Logs.Where(log => MatchesLog(log, serviceResourceIds)))
                TryWrite(new OpenTelemetryStreamItem { Log = log });

            foreach (var point in batch.MetricPoints.Where(point => MatchesMetricPoint(point, serviceResourceIds)))
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

        private bool MatchesResource(TelemetryResource resource)
        {
            if (!string.IsNullOrWhiteSpace(filter.TraceId))
                return false;

            if (!string.IsNullOrWhiteSpace(filter.WorkflowInstanceId))
                return false;

            if (filter.Status != null)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.ResourceId) && !EqualsIgnoreCase(resource.Id, filter.ResourceId))
                return false;

            if (!string.IsNullOrWhiteSpace(filter.ServiceName) && !EqualsIgnoreCase(resource.ServiceName, filter.ServiceName))
                return false;

            if (filter.From != null && resource.LastSeen < filter.From)
                return false;

            if (filter.To != null && resource.LastSeen > filter.To)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.Search) && !Matches(resource.Id, filter.Search) && !Matches(resource.ServiceName, filter.Search))
                return false;

            return true;
        }

        private bool MatchesTrace(TelemetryTrace trace, HashSet<string>? serviceResourceIds)
        {
            if (!string.IsNullOrWhiteSpace(filter.TraceId) && !EqualsIgnoreCase(trace.TraceId, filter.TraceId))
                return false;

            if (filter.Status != null && trace.Status != filter.Status)
                return false;

            if (filter.From != null && trace.StartTime < filter.From)
                return false;

            if (filter.To != null && trace.StartTime > filter.To)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.WorkflowInstanceId) && !trace.WorkflowInstanceIds.Any(id => Matches(id, filter.WorkflowInstanceId)))
                return false;

            if (!string.IsNullOrWhiteSpace(filter.ResourceId) && !trace.ResourceIds.Contains(filter.ResourceId, StringComparer.OrdinalIgnoreCase))
                return false;

            if (serviceResourceIds != null && !trace.ResourceIds.Any(serviceResourceIds.Contains))
                return false;

            if (!string.IsNullOrWhiteSpace(filter.Search) && !Matches(trace.TraceId, filter.Search) && !Matches(trace.Name, filter.Search))
                return false;

            return true;
        }

        private bool MatchesLog(OtlpLogRecord log, HashSet<string>? serviceResourceIds)
        {
            if (filter.Status != null)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.ResourceId) && !EqualsIgnoreCase(log.ResourceId, filter.ResourceId))
                return false;

            if (serviceResourceIds != null && !serviceResourceIds.Contains(log.ResourceId))
                return false;

            if (!string.IsNullOrWhiteSpace(filter.TraceId) && !EqualsIgnoreCase(log.TraceId, filter.TraceId))
                return false;

            if (!string.IsNullOrWhiteSpace(filter.WorkflowInstanceId) && !AttributeMatches(log.Attributes, "workflow.instance.id", filter.WorkflowInstanceId))
                return false;

            if (filter.From != null && log.Timestamp < filter.From)
                return false;

            if (filter.To != null && log.Timestamp > filter.To)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.Search) && !Matches(log.Body, filter.Search))
                return false;

            return true;
        }

        private bool MatchesMetricPoint(MetricPoint point, HashSet<string>? serviceResourceIds)
        {
            if (filter.Status != null)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.ResourceId) && !EqualsIgnoreCase(point.ResourceId, filter.ResourceId))
                return false;

            if (serviceResourceIds != null && !serviceResourceIds.Contains(point.ResourceId))
                return false;

            if (!string.IsNullOrWhiteSpace(filter.TraceId) && !EqualsIgnoreCase(point.TraceId, filter.TraceId))
                return false;

            if (!string.IsNullOrWhiteSpace(filter.WorkflowInstanceId) && !AttributeMatches(point.Attributes, "workflow.instance.id", filter.WorkflowInstanceId))
                return false;

            if (filter.From != null && point.Timestamp < filter.From)
                return false;

            if (filter.To != null && point.Timestamp > filter.To)
                return false;

            if (!string.IsNullOrWhiteSpace(filter.Search) && !Matches(point.InstrumentName, filter.Search) && !Matches(point.InstrumentId, filter.Search))
                return false;

            return true;
        }

        private HashSet<string>? ResolveServiceResourceIds(IEnumerable<TelemetryResource> resources)
        {
            if (string.IsNullOrWhiteSpace(filter.ServiceName))
                return null;

            return resources
                .Where(x => EqualsIgnoreCase(x.ServiceName, filter.ServiceName))
                .Select(x => x.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static bool AttributeMatches(IDictionary<string, string?> attributes, string key, string? search)
        {
            return attributes.TryGetValue(key, out var value) && Matches(value, search);
        }

        private static bool EqualsIgnoreCase(string? candidate, string? search) => string.Equals(candidate, search, StringComparison.OrdinalIgnoreCase);

        private static bool Matches(string? candidate, string? search) => !string.IsNullOrEmpty(candidate) && !string.IsNullOrEmpty(search) && candidate.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
