# Data Model: Diagnostics OpenTelemetry

## Telemetry Resource

Represents a service, process, container, or resource that emits telemetry.

| Field | Notes |
|-------|-------|
| `ResourceKey` | Stable backend key derived from service name, instance ID, and resource attributes. |
| `ServiceName` | From `service.name` or `OTEL_SERVICE_NAME`; fallback is backend-defined. |
| `ServiceInstanceId` | From `service.instance.id` when present. |
| `ServiceVersion` | From `service.version` when present. |
| `DeploymentEnvironment` | From `deployment.environment` when present. |
| `Attributes` | Redacted resource attributes. |
| `FirstSeen`, `LastSeen` | Backend receive timestamps. |
| `Status` | Active, stale, or disconnected based on receive heartbeat. |

## Telemetry Trace

Represents a trace summary.

| Field | Notes |
|-------|-------|
| `TraceId` | Required trace identifier. |
| `RootSpanId` | Root span when known. |
| `Name` | Root span name or synthesized trace label. |
| `ResourceKeys` | Resources participating in the trace. |
| `StartTime`, `EndTime`, `Duration` | Derived from spans. |
| `Status` | Ok, error, unset, or mixed. |
| `SpanCount`, `ErrorCount` | Derived counters. |
| `WorkflowInstanceId`, `WorkflowDefinitionId` | Populated from Elsa span attributes when present. |
| `LastReceivedAt` | Backend receive timestamp for ordering. |

## Telemetry Span

Represents one operation inside a trace.

| Field | Notes |
|-------|-------|
| `TraceId`, `SpanId`, `ParentSpanId` | Trace hierarchy. |
| `ResourceKey` | Emitting resource. |
| `Name`, `Kind` | OTEL span metadata. |
| `StartTime`, `EndTime`, `Duration` | Timing. |
| `StatusCode`, `StatusDescription` | OTEL status. |
| `Attributes` | Redacted span attributes. |
| `Events` | Redacted span events, including exception markers. |
| `Links` | Span links. |
| `Workflow*`, `Activity*`, `TenantId` | Elsa semantic attributes when present. |
| `ReceivedAt` | Backend receive timestamp. |

## Metric Instrument

Represents an OTEL metric series group.

| Field | Notes |
|-------|-------|
| `Name` | Instrument name. |
| `Description` | Instrument description. |
| `Unit` | Instrument unit. |
| `Type` | Sum, gauge, histogram, exponential histogram, or summary if supported. |
| `ResourceKey` | Emitting resource. |
| `Series` | Bounded collection of attribute-set series. |
| `DroppedPointCount` | Points dropped because of configured capacity. |

## Metric Point

Represents one recent metric data point.

| Field | Notes |
|-------|-------|
| `Timestamp` | Point timestamp or receive time fallback. |
| `StartTimestamp` | Cumulative metric start time when present. |
| `Attributes` | Redacted point attributes. |
| `Value` | Numeric value for sum/gauge points. |
| `Count`, `Sum`, `Min`, `Max`, `Buckets` | Histogram fields when present. |
| `Temporality` | Cumulative or delta. |

## OTLP Log Record

Represents a log received through OTLP.

| Field | Notes |
|-------|-------|
| `Id` | Backend assigned ID. |
| `Timestamp`, `ObservedTimestamp`, `ReceivedAt` | OTLP and backend timing. |
| `ResourceKey` | Emitting resource. |
| `SeverityText`, `SeverityNumber` | OTEL severity. |
| `Body` | Redacted body text. |
| `TraceId`, `SpanId` | Correlation fields. |
| `Attributes` | Redacted log attributes. |

## Collector Configuration

Represents active collector setup.

| Field | Notes |
|-------|-------|
| `HttpEndpoint` | Base endpoint suitable for `OTEL_EXPORTER_OTLP_ENDPOINT` with HTTP/protobuf. |
| `GrpcEndpoint` | Endpoint suitable for gRPC when enabled; null when unavailable. |
| `GrpcEnabled` | Whether gRPC ingestion is currently enabled. |
| `GrpcDisabledReason` | Optional non-secret explanation when gRPC ingestion is unavailable. |
| `RequiredHeaders` | Header names required for ingestion, without exposing secret values. |
| `RecommendedEnvironment` | Copyable environment variable names and non-secret values. |
| `IsLoopbackOnly` | Whether current listener is restricted to loopback. |

## Storage Diagnostics

Reports capacity and health.

| Field | Notes |
|-------|-------|
| `DroppedSpanCount` | Spans dropped by store capacity. |
| `DroppedMetricPointCount` | Metric points dropped by store capacity. |
| `DroppedLogRecordCount` | OTLP log records dropped by store capacity. |
| `DroppedLiveUpdateCount` | Live updates dropped by subscriber capacity. |
| `ResourceCount` | Current resource count. |
| `TraceCount` | Current trace count. |
| `MetricSeriesCount` | Current metric series count. |
