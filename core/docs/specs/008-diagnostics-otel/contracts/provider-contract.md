# Contract: Provider and Store

## Provider Facade

```csharp
public interface IOpenTelemetryProvider
{
    ValueTask<SearchTelemetryResourcesResult> SearchResourcesAsync(TelemetryResourceFilter filter, CancellationToken cancellationToken = default);
    ValueTask<SearchTelemetryTracesResult> SearchTracesAsync(TelemetryTraceFilter filter, CancellationToken cancellationToken = default);
    ValueTask<TelemetryTraceDetail?> GetTraceAsync(string traceId, CancellationToken cancellationToken = default);
    ValueTask<SearchTelemetryMetricsResult> SearchMetricsAsync(TelemetryMetricFilter filter, CancellationToken cancellationToken = default);
    ValueTask<SearchOtlpLogsResult> SearchLogsAsync(OtlpLogFilter filter, CancellationToken cancellationToken = default);
    ValueTask<OpenTelemetryStorageDiagnostics> GetStorageDiagnosticsAsync(CancellationToken cancellationToken = default);
}
```

## Store

```csharp
public interface IOpenTelemetryStore
{
    ValueTask WriteTracesAsync(IReadOnlyCollection<TelemetrySpan> spans, CancellationToken cancellationToken = default);
    ValueTask WriteMetricsAsync(IReadOnlyCollection<MetricInstrumentUpdate> metrics, CancellationToken cancellationToken = default);
    ValueTask WriteLogsAsync(IReadOnlyCollection<OtlpLogRecord> logs, CancellationToken cancellationToken = default);
}
```

The concrete in-memory implementation also supports provider queries. A later durable provider may split write/query responsibilities if needed.

Search methods return bounded results using server-capped limits. Search ordering is newest receive time first. Trace detail ordering is parent/child chronological so Studio can render a waterfall without reassembling hierarchy from arbitrary span order.

## Redaction

```csharp
public interface IOpenTelemetryRedactor
{
    TelemetryResource Redact(TelemetryResource resource);
    TelemetrySpan Redact(TelemetrySpan span);
    MetricPoint Redact(MetricPoint point);
    OtlpLogRecord Redact(OtlpLogRecord logRecord);
}
```

Redaction occurs before store writes and live publishing.

## Capacity

The default in-memory provider uses configurable per-signal limits with drop-oldest overflow. Defaults must be at least 500 resources, 2,000 traces, 10,000 spans, 20,000 metric points, 10,000 OTLP log records, and 1,000 queued live updates per subscriber.
