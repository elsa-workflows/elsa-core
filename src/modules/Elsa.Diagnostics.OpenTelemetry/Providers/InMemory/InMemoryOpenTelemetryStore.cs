using Elsa.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Diagnostics.OpenTelemetry.Models;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.OpenTelemetry.Providers.InMemory;

public class InMemoryOpenTelemetryStore : IOpenTelemetryStore
{
    private readonly IOpenTelemetrySourceRegistry _sourceRegistry;
    private readonly OpenTelemetryDiagnosticsOptions _options;
    private readonly RingBuffer<TelemetryTrace> _traces;
    private readonly RingBuffer<TelemetrySpan> _spans;
    private readonly RingBuffer<MetricPoint> _metricPoints;
    private readonly RingBuffer<OtlpLogRecord> _logs;
    private readonly object _instrumentLock = new();
    private readonly Dictionary<string, MetricInstrument> _instruments = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryOpenTelemetryStore(IOptions<OpenTelemetryDiagnosticsOptions> options, IOpenTelemetrySourceRegistry sourceRegistry)
    {
        _options = options.Value;
        _sourceRegistry = sourceRegistry;
        _traces = new(ClampCapacity(_options.TraceCapacity));
        _spans = new(ClampCapacity(_options.SpanCapacity));
        _metricPoints = new(ClampCapacity(_options.MetricPointCapacity));
        _logs = new(ClampCapacity(_options.LogRecordCapacity));
    }

    public ValueTask WriteAsync(OpenTelemetryBatch batch, CancellationToken cancellationToken = default)
    {
        foreach (var resource in batch.Resources)
            _sourceRegistry.MarkSeen(resource);

        foreach (var trace in batch.Traces)
            _traces.Add(trace);

        foreach (var span in batch.Spans)
            _spans.Add(span);

        lock (_instrumentLock)
        {
            foreach (var instrument in batch.Instruments)
                _instruments[instrument.Id] = instrument;
        }

        foreach (var point in batch.MetricPoints)
            _metricPoints.Add(point);

        foreach (var log in batch.Logs)
            _logs.Add(log);

        return ValueTask.CompletedTask;
    }

    public ValueTask<OpenTelemetryResourceResult> QueryResourcesAsync(OpenTelemetryResourceFilter filter, CancellationToken cancellationToken = default)
    {
        var take = ClampTake(filter.Take);
        var items = _sourceRegistry.List()
            .Where(x => string.IsNullOrWhiteSpace(filter.ServiceName) || string.Equals(x.ServiceName, filter.ServiceName, StringComparison.OrdinalIgnoreCase))
            .Where(x => filter.Status == null || x.Status == filter.Status)
            .Where(x => string.IsNullOrWhiteSpace(filter.Search) || Matches(x.ServiceName, filter.Search) || Matches(x.Id, filter.Search))
            .OrderByDescending(x => x.LastSeen)
            .ThenBy(x => x.ServiceName, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();

        return ValueTask.FromResult(new OpenTelemetryResourceResult(items, _sourceRegistry.DroppedCount));
    }

    public ValueTask<OpenTelemetryTraceResult> QueryTracesAsync(OpenTelemetryTraceFilter filter, CancellationToken cancellationToken = default)
    {
        var take = ClampTake(filter.Take);
        var serviceResourceIds = ResolveResourceIds(filter.ServiceName);
        var items = _traces.Snapshot()
            .Where(x => string.IsNullOrWhiteSpace(filter.TraceId) || Matches(x.TraceId, filter.TraceId))
            .Where(x => filter.Status == null || x.Status == filter.Status)
            .Where(x => filter.From == null || x.StartTime >= filter.From)
            .Where(x => filter.To == null || x.StartTime <= filter.To)
            .Where(x => string.IsNullOrWhiteSpace(filter.WorkflowInstanceId) || x.WorkflowInstanceIds.Any(id => Matches(id, filter.WorkflowInstanceId)))
            .Where(x => string.IsNullOrWhiteSpace(filter.ResourceId) || x.ResourceIds.Contains(filter.ResourceId, StringComparer.OrdinalIgnoreCase))
            .Where(x => serviceResourceIds == null || x.ResourceIds.Any(serviceResourceIds.Contains))
            .Where(x => string.IsNullOrWhiteSpace(filter.Search) || Matches(x.TraceId, filter.Search) || Matches(x.Name, filter.Search))
            .OrderBy(x => x.StartTime)
            .TakeLast(take)
            .ToList();

        return ValueTask.FromResult(new OpenTelemetryTraceResult(items, _traces.DroppedCount));
    }

    public ValueTask<OpenTelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default)
    {
        var trace = _traces.Snapshot().LastOrDefault(x => string.Equals(x.TraceId, traceId, StringComparison.OrdinalIgnoreCase));
        if (trace == null)
            return ValueTask.FromResult<OpenTelemetryTraceDetail?>(null);

        var spans = _spans.Snapshot()
            .Where(x => string.Equals(x.TraceId, traceId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.StartTime)
            .ThenBy(x => x.SpanId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var resourceIds = trace.ResourceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var resources = _sourceRegistry.List()
            .Where(x => resourceIds.Contains(x.Id))
            .OrderBy(x => x.ServiceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var logs = _logs.Snapshot()
            .Where(x => string.Equals(x.TraceId, traceId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Timestamp)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ValueTask.FromResult<OpenTelemetryTraceDetail?>(new OpenTelemetryTraceDetail(trace, spans, resources, logs));
    }

    public ValueTask<OpenTelemetryMetricResult> QueryMetricsAsync(OpenTelemetryMetricFilter filter, CancellationToken cancellationToken = default)
    {
        var take = ClampTake(filter.Take);
        var serviceResourceIds = ResolveResourceIds(filter.ServiceName);
        var points = _metricPoints.Snapshot()
            .Where(x => string.IsNullOrWhiteSpace(filter.ResourceId) || string.Equals(x.ResourceId, filter.ResourceId, StringComparison.OrdinalIgnoreCase))
            .Where(x => serviceResourceIds == null || serviceResourceIds.Contains(x.ResourceId))
            .Where(x => filter.From == null || x.Timestamp >= filter.From)
            .Where(x => filter.To == null || x.Timestamp <= filter.To)
            .OrderBy(x => x.Timestamp)
            .TakeLast(take)
            .ToList();

        Dictionary<string, MetricInstrument> instruments;
        lock (_instrumentLock)
            instruments = new(_instruments, StringComparer.OrdinalIgnoreCase);

        var selectedInstruments = points
            .Select(x => x.InstrumentId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(instruments.ContainsKey)
            .Select(x => instruments[x])
            .Where(x => string.IsNullOrWhiteSpace(filter.InstrumentName) || Matches(x.Name, filter.InstrumentName))
            .ToList();

        if (!string.IsNullOrWhiteSpace(filter.InstrumentName))
        {
            var instrumentIds = selectedInstruments.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            points = points.Where(x => instrumentIds.Contains(x.InstrumentId)).ToList();
        }

        return ValueTask.FromResult(new OpenTelemetryMetricResult(selectedInstruments, points, _metricPoints.DroppedCount));
    }

    public ValueTask<OpenTelemetryLogResult> QueryLogsAsync(OpenTelemetryLogFilter filter, CancellationToken cancellationToken = default)
    {
        var take = ClampTake(filter.Take);
        var serviceResourceIds = ResolveResourceIds(filter.ServiceName);
        var logs = _logs.Snapshot()
            .Where(x => string.IsNullOrWhiteSpace(filter.ResourceId) || string.Equals(x.ResourceId, filter.ResourceId, StringComparison.OrdinalIgnoreCase))
            .Where(x => serviceResourceIds == null || serviceResourceIds.Contains(x.ResourceId))
            .Where(x => string.IsNullOrWhiteSpace(filter.TraceId) || Matches(x.TraceId, filter.TraceId))
            .Where(x => string.IsNullOrWhiteSpace(filter.SpanId) || Matches(x.SpanId, filter.SpanId))
            .Where(x => string.IsNullOrWhiteSpace(filter.Severity) || Matches(x.SeverityText, filter.Severity))
            .Where(x => filter.From == null || x.Timestamp >= filter.From)
            .Where(x => filter.To == null || x.Timestamp <= filter.To)
            .Where(x => string.IsNullOrWhiteSpace(filter.Search) || Matches(x.Body, filter.Search))
            .OrderBy(x => x.Timestamp)
            .TakeLast(take)
            .ToList();

        return ValueTask.FromResult(new OpenTelemetryLogResult(logs, _logs.DroppedCount));
    }

    public ValueTask<OpenTelemetryStorageDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
    {
        Dictionary<string, MetricInstrument> instruments;
        lock (_instrumentLock)
            instruments = new(_instruments, StringComparer.OrdinalIgnoreCase);

        var traceSnapshot = _traces.Snapshot();
        var spanSnapshot = _spans.Snapshot();
        var metricPointSnapshot = _metricPoints.Snapshot();
        var logSnapshot = _logs.Snapshot();
        var diagnostics = new OpenTelemetryStorageDiagnostics(
            _options.TraceCapacity,
            _options.SpanCapacity,
            _options.MetricPointCapacity,
            _options.LogRecordCapacity,
            _sourceRegistry.List().Count,
            traceSnapshot.Count,
            spanSnapshot.Count,
            instruments.Count,
            metricPointSnapshot.Count,
            logSnapshot.Count,
            _traces.DroppedCount,
            _spans.DroppedCount,
            _metricPoints.DroppedCount,
            _logs.DroppedCount);

        return ValueTask.FromResult(diagnostics);
    }

    private int ClampTake(int? take) => Math.Clamp(take ?? _options.MaxQuerySize, 0, _options.MaxQuerySize);

    private static int ClampCapacity(int capacity) => Math.Max(1, capacity);

    private HashSet<string>? ResolveResourceIds(string? serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return null;

        return _sourceRegistry.List()
            .Where(x => string.Equals(x.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool Matches(string? candidate, string? search) => !string.IsNullOrEmpty(candidate) && !string.IsNullOrEmpty(search) && candidate.Contains(search, StringComparison.OrdinalIgnoreCase);
}
