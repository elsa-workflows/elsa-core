# Data Model: Diagnostics OpenTelemetry

## Telemetry Resource

| Field | Notes |
|-------|-------|
| `ResourceKey` | Stable key derived from service name, instance ID, and resource attributes. |
| `ServiceName` | From `service.name` or `OTEL_SERVICE_NAME`; fallback is `unknown_service`. |
| `ServiceInstanceId` | From `service.instance.id` when present. |
| `ServiceVersion` | From `service.version` when present. |
| `DeploymentEnvironment` | From `deployment.environment` when present. |
| `Attributes` | Redacted resource attributes. |
| `FirstSeen`, `LastSeen` | Backend receive timestamps. |
| `Status` | Active, stale, or disconnected. |

## Telemetry Trace

| Field | Notes |
|-------|-------|
| `TraceId` | Required trace identifier. |
| `RootSpanId` | Root span when known. |
| `Name` | Root span name or synthesized trace label. |
| `ResourceKeys` | Participating resources. |
| `StartTime`, `EndTime`, `Duration` | Derived from spans. |
| `Status` | Ok, error, unset, or mixed. |
| `SpanCount`, `ErrorCount` | Derived counters. |
| `WorkflowInstanceId`, `WorkflowDefinitionId` | Elsa span attributes when present. |
| `LastReceivedAt` | Backend receive timestamp. |

## Telemetry Span

| Field | Notes |
|-------|-------|
| `TraceId`, `SpanId`, `ParentSpanId` | Trace hierarchy. |
| `ResourceKey` | Emitting resource. |
| `Name`, `Kind` | OTEL span metadata. |
| `StartTime`, `EndTime`, `Duration` | Timing. |
| `StatusCode`, `StatusDescription` | OTEL status. |
| `Attributes` | Redacted span attributes. |
| `Events` | Redacted events. |
| `Links` | Span links. |
| `Workflow*`, `Activity*`, `TenantId`, `CorrelationId` | Existing `Elsa.Workflows` semantic attributes when present: `workflow.*`, `workflow.activity.*`, and `elsa.tenant.id`. |
| `ReceivedAt` | Backend receive timestamp. |

## Metric Instrument and Point

| Field | Notes |
|-------|-------|
| `Name`, `Description`, `Unit`, `Type` | Instrument metadata. |
| `ResourceKey` | Emitting resource. |
| `Series` | Bounded collection by attribute set. |
| `Timestamp`, `StartTimestamp` | Point timestamps. |
| `Attributes` | Redacted point attributes. |
| `Value` | Sum/gauge value. |
| `Count`, `Sum`, `Min`, `Max`, `Buckets` | Histogram fields when present. |
| `Temporality` | Cumulative or delta. |
| `DroppedPointCount` | Capacity drop count. |

## OTLP Log Record

| Field | Notes |
|-------|-------|
| `Id` | Backend assigned ID. |
| `Timestamp`, `ObservedTimestamp`, `ReceivedAt` | OTLP and backend timing. |
| `ResourceKey` | Emitting resource. |
| `SeverityText`, `SeverityNumber` | OTEL severity. |
| `Body` | Redacted body. |
| `TraceId`, `SpanId` | Correlation fields. |
| `Attributes` | Redacted log attributes. |

## Collector Configuration

| Field | Notes |
|-------|-------|
| `HttpEndpoint` | Base endpoint suitable for `OTEL_EXPORTER_OTLP_ENDPOINT` with HTTP/protobuf. |
| `GrpcEndpoint` | Endpoint suitable for gRPC when enabled; null when unavailable. |
| `GrpcEnabled` | Whether gRPC ingestion is enabled. |
| `GrpcDisabledReason` | Optional non-secret explanation when disabled. |
| `RequiredHeaders` | Required ingestion header names without secret values. |
| `RecommendedEnvironment` | Copyable non-secret OTEL environment variable values. |
| `IsLoopbackOnly` | Whether ingestion is restricted to loopback. |
| `RequiresApiKey` | Whether configured ingestion protection is required for advertised endpoints. |

## Storage Diagnostics

| Field | Notes |
|-------|-------|
| `DroppedSpanCount` | Spans dropped by store capacity. |
| `DroppedMetricPointCount` | Metric points dropped by capacity. |
| `DroppedLogRecordCount` | OTLP logs dropped by capacity. |
| `DroppedLiveUpdateCount` | Live updates dropped by subscriber capacity. |
| `ResourceCount`, `TraceCount`, `MetricSeriesCount` | Current counts. |

## Storage Capacity Policy

| Field | Notes |
|-------|-------|
| `MaxResources` | Default at least 500. |
| `MaxTraces` | Default at least 2,000. |
| `MaxSpans` | Default at least 10,000. |
| `MaxMetricPoints` | Default at least 20,000 across all metric series. |
| `MaxLogRecords` | Default at least 10,000. |
| `MaxLiveUpdatesPerSubscriber` | Default at least 1,000 queued updates per SignalR subscriber. |
| `OverflowPolicy` | Drop oldest item in the signal-specific buffer and increment the matching dropped counter. |
