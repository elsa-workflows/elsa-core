namespace Elsa.Diagnostics.OpenTelemetry.Models;

public enum OpenTelemetrySignalType
{
    Trace,
    Metric,
    Log
}

public enum TelemetryResourceStatus
{
    Unknown,
    Active,
    Stale
}

public enum SpanStatus
{
    Unset,
    Ok,
    Error
}

public enum MetricKind
{
    Gauge,
    Sum,
    Histogram
}

public record TelemetryResource(
    string Id,
    string ServiceName,
    string? ServiceInstanceId,
    string? TelemetrySdkLanguage,
    IDictionary<string, string?> Attributes,
    DateTimeOffset LastSeen,
    TelemetryResourceStatus Status);

public record TelemetrySpan(
    string Id,
    string TraceId,
    string SpanId,
    string? ParentSpanId,
    string ResourceId,
    string Name,
    string Kind,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    SpanStatus Status,
    string? StatusDescription,
    IDictionary<string, string?> Attributes,
    IReadOnlyCollection<TelemetrySpanEvent> Events,
    IReadOnlyCollection<TelemetrySpanLink> Links);

public record TelemetrySpanEvent(
    string Name,
    DateTimeOffset Timestamp,
    IDictionary<string, string?> Attributes);

public record TelemetrySpanLink(
    string TraceId,
    string SpanId,
    IDictionary<string, string?> Attributes);

public record TelemetryTrace(
    string TraceId,
    string? RootSpanId,
    string? Name,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    TimeSpan Duration,
    SpanStatus Status,
    IReadOnlyCollection<string> ResourceIds,
    IReadOnlyCollection<string> WorkflowInstanceIds,
    int SpanCount);

public record MetricInstrument(
    string Id,
    string ResourceId,
    string Name,
    string? Unit,
    string? Description,
    MetricKind Kind,
    IDictionary<string, string?> Attributes);

public record MetricPoint(
    string Id,
    string InstrumentId,
    string InstrumentName,
    string ResourceId,
    DateTimeOffset Timestamp,
    double? Value,
    double? Sum,
    long? Count,
    IDictionary<string, string?> Attributes,
    string? TraceId,
    string? SpanId);

public record OtlpLogRecord(
    string Id,
    string ResourceId,
    DateTimeOffset Timestamp,
    string SeverityText,
    int? SeverityNumber,
    string Body,
    string? TraceId,
    string? SpanId,
    IDictionary<string, string?> Attributes);

public record OpenTelemetryDroppedItemSummary(OpenTelemetrySignalType SignalType, long Count, string Reason);

public record OpenTelemetryStorageDiagnostics(
    int TraceCapacity,
    int SpanCapacity,
    int MetricPointCapacity,
    int LogRecordCapacity,
    int ResourceCount,
    int TraceCount,
    int SpanCount,
    int MetricInstrumentCount,
    int MetricPointCount,
    int LogRecordCount,
    long DroppedTraceCount,
    long DroppedSpanCount,
    long DroppedMetricPointCount,
    long DroppedLogRecordCount);
