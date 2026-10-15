# Contract: OTLP Ingestion

## HTTP/protobuf

Default base path:

```text
/elsa/otlp/v1
```

Supported paths:

```text
POST /elsa/otlp/v1/traces
POST /elsa/otlp/v1/metrics
POST /elsa/otlp/v1/logs
```

Requests use OTLP protobuf bodies. Accepted payloads return `200 OK`; unauthorized requests return `401 Unauthorized`; malformed protobuf payloads return `400 Bad Request`; payloads over the configured request body limit return `413 Payload Too Large`. The current collector does not emit OTLP partial-success response bodies.

## gRPC

When enabled, Core exposes OTLP gRPC services:

```text
opentelemetry.proto.collector.trace.v1.TraceService/Export
opentelemetry.proto.collector.metrics.v1.MetricsService/Export
opentelemetry.proto.collector.logs.v1.LogsService/Export
```

When gRPC is disabled, collector configuration must return `GrpcEnabled = false` and no endpoint.

## Security

- Loopback-only development ingestion may run without an API key only when the request remote address is loopback and the advertised collector binding is loopback.
- Non-loopback requests and non-loopback collector bindings must require a configured header such as `x-otlp-api-key`.
- Secret header values are configured server-side, compared without exposing the configured value, and omitted from collector configuration responses.
- Accepted telemetry is redacted before storage and live streaming.
- OTLP sender auth is separate from diagnostics API user auth.

## Internal Contract

```csharp
public interface IOpenTelemetryIngestor
{
    ValueTask IngestAsync(OpenTelemetryBatch batch, CancellationToken cancellationToken = default);
}
```

Transport-specific code parses payloads; shared ingestion normalizes, redacts, stores, and publishes live updates.
