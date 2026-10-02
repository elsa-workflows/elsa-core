# Contract: OTLP Ingestion

## Purpose

`Elsa.Diagnostics.OpenTelemetry` accepts standard OTLP payloads and normalizes them into the diagnostics telemetry store.

## HTTP/protobuf

Default base path:

```text
/elsa/otlp
```

Supported OTLP HTTP paths:

```text
POST /elsa/otlp/v1/traces
POST /elsa/otlp/v1/metrics
POST /elsa/otlp/v1/logs
```

Request body is OTLP protobuf for the corresponding signal. Response follows OTLP success/failure semantics and should include partial success details when supported by the SDK/proto model.

Recommended sender configuration:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:<port>/elsa/otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_SERVICE_NAME=<service-name>
OTEL_RESOURCE_ATTRIBUTES=service.instance.id=<instance-id>
```

## gRPC

When enabled, the same module exposes OTLP gRPC services for:

```text
opentelemetry.proto.collector.trace.v1.TraceService/Export
opentelemetry.proto.collector.metrics.v1.MetricsService/Export
opentelemetry.proto.collector.logs.v1.LogsService/Export
```

Recommended sender configuration:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:<grpc-port>
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
OTEL_SERVICE_NAME=<service-name>
OTEL_RESOURCE_ATTRIBUTES=service.instance.id=<instance-id>
```

## Security

- Loopback-only development ingestion may run without an API key.
- Non-loopback ingestion must require a configured header such as `x-otlp-api-key`.
- Accepted telemetry is redacted before storage and live streaming.
- OTLP sender auth is separate from Studio user auth; Studio-facing APIs remain user-authenticated.

## Internal Ingest Contract

Both transports feed one internal ingestor:

```csharp
public interface IOpenTelemetryIngestor
{
    ValueTask<OpenTelemetryIngestResult> IngestTracesAsync(OtlpTraceBatch batch, CancellationToken cancellationToken = default);
    ValueTask<OpenTelemetryIngestResult> IngestMetricsAsync(OtlpMetricBatch batch, CancellationToken cancellationToken = default);
    ValueTask<OpenTelemetryIngestResult> IngestLogsAsync(OtlpLogBatch batch, CancellationToken cancellationToken = default);
}
```

`Otlp*Batch` names are placeholders for the module's OTLP protobuf wrapper/read model. The important boundary is that transport-specific code parses payloads only; normalization, redaction, storage, and live publishing are shared.
