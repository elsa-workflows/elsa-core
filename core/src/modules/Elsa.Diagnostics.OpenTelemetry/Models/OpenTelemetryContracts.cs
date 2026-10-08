namespace Elsa.Diagnostics.OpenTelemetry.Models;

public record OpenTelemetryResourceFilter
{
    public string? Search { get; init; }
    public string? ServiceName { get; init; }
    public TelemetryResourceStatus? Status { get; init; }
    public int? Take { get; init; }
}

public record OpenTelemetryTraceFilter
{
    public string? ResourceId { get; init; }
    public string? ServiceName { get; init; }
    public string? TraceId { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public SpanStatus? Status { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? Search { get; init; }
    public int? Take { get; init; }
}

public record OpenTelemetryMetricFilter
{
    public string? ResourceId { get; init; }
    public string? ServiceName { get; init; }
    public string? InstrumentName { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? Take { get; init; }
}

public record OpenTelemetryLogFilter
{
    public string? ResourceId { get; init; }
    public string? ServiceName { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? Severity { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? Search { get; init; }
    public int? Take { get; init; }
}

public record OpenTelemetryResourceResult(IReadOnlyCollection<TelemetryResource> Items, long DroppedCount);

public record OpenTelemetryTraceResult(IReadOnlyCollection<TelemetryTrace> Items, long DroppedCount);

public record OpenTelemetryTraceDetail(
    TelemetryTrace Trace,
    IReadOnlyCollection<TelemetrySpan> Spans,
    IReadOnlyCollection<TelemetryResource> Resources,
    IReadOnlyCollection<OtlpLogRecord> Logs);

public record OpenTelemetryMetricResult(IReadOnlyCollection<MetricInstrument> Instruments, IReadOnlyCollection<MetricPoint> Points, long DroppedCount);

public record OpenTelemetryLogResult(IReadOnlyCollection<OtlpLogRecord> Items, long DroppedCount);

public record CollectorEndpointInfo(string Protocol, string? Endpoint, bool Enabled, string? DisabledReason);

public record CollectorConfiguration(
    CollectorEndpointInfo Http,
    CollectorEndpointInfo Grpc,
    string ServiceNameEnvironmentVariable,
    string EndpointEnvironmentVariable,
    string ProtocolEnvironmentVariable,
    IReadOnlyDictionary<string, string> RequiredHeaders);

public record OpenTelemetryBatch(
    IReadOnlyCollection<TelemetryResource> Resources,
    IReadOnlyCollection<TelemetryTrace> Traces,
    IReadOnlyCollection<TelemetrySpan> Spans,
    IReadOnlyCollection<MetricInstrument> Instruments,
    IReadOnlyCollection<MetricPoint> MetricPoints,
    IReadOnlyCollection<OtlpLogRecord> Logs);

public record OpenTelemetryStreamItem
{
    public TelemetryResource? Resource { get; init; }
    public TelemetryTrace? Trace { get; init; }
    public MetricPoint? MetricPoint { get; init; }
    public OtlpLogRecord? Log { get; init; }
    public OpenTelemetryDroppedItemSummary? DroppedItems { get; init; }
}
