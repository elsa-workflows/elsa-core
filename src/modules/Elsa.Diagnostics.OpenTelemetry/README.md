# Elsa Diagnostics OpenTelemetry

Provides the OpenTelemetry diagnostics collector surface for Elsa hosts.

This module owns the Core-side OTEL contracts, HTTP/protobuf ingestion, bounded in-memory diagnostics store, feature registration, permissions, read APIs, live update hub, and collector metadata that Studio consumes.

## Scope

`Elsa.Diagnostics.OpenTelemetry` is collector/read-side diagnostics infrastructure. It does not create workflow spans, mutate `Activity.Current`, export telemetry to vendors, or provide durable OpenTelemetry persistence. Workflow telemetry production remains owned by existing `Elsa.Workflows` `ActivitySource` and `Meter` instrumentation.

The historical `Elsa.OpenTelemetry` module from `elsa-extensions` is intentionally not ported into this feature. It is producer-side workflow tracing middleware, duplicates current `Elsa.Workflows` instrumentation, and mutates `Activity.Current`; this module is collector/read-side diagnostics infrastructure.

## Routes

OTLP HTTP/protobuf ingestion is exposed under `OpenTelemetryDiagnosticsOptions.HttpEndpointPath`, which defaults to `/elsa/otlp/v1`:

- `POST /elsa/otlp/v1/traces`
- `POST /elsa/otlp/v1/metrics`
- `POST /elsa/otlp/v1/logs`

Diagnostics read APIs are exposed through Elsa API endpoints:

- `POST /diagnostics/opentelemetry/resources/search`
- `POST /diagnostics/opentelemetry/traces/search`
- `GET /diagnostics/opentelemetry/traces/{traceId}`
- `POST /diagnostics/opentelemetry/metrics/search`
- `POST /diagnostics/opentelemetry/logs/search`
- `GET /diagnostics/opentelemetry/storage`
- `GET /diagnostics/opentelemetry/collector-configuration`

Live updates are exposed through SignalR at `OpenTelemetryDiagnosticsOptions.HubRoute`, which defaults to `/elsa/hubs/diagnostics/opentelemetry`.

## Security

All diagnostics read APIs require the OpenTelemetry diagnostics read permission. OTLP ingestion allows unauthenticated loopback traffic by default for local development. Non-loopback senders must provide the configured API key header when `OpenTelemetryDiagnosticsOptions.ApiKey` is set; requests without a configured key are rejected unless they are loopback requests and loopback development mode is enabled.

Collector configuration returns endpoint and required-header names only. It never returns secret header values.

## Storage

The default provider is bounded in-memory storage. Capacity options cover traces, spans, metric points, OTLP log records, and live subscriber queues. When a buffer exceeds capacity, the oldest item for that signal is dropped and diagnostics counters are incremented.
