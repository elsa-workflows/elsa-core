# Research: Diagnostics OpenTelemetry

## Decision: Pair a Core collector module with a Studio viewer module

**Rationale**: Elsa Studio already treats the backend as the capability boundary. Core can receive OTLP, normalize telemetry, apply permissions, and expose APIs; Studio can remain a thin authenticated viewer. This mirrors Aspire's collector/API/UI split without requiring Studio to own process launch.

**Alternatives considered**:

- Studio as the OTLP collector: rejected because browser-hosted Studio cannot reliably expose OTLP endpoints, authenticate senders, or collect backend-local telemetry.
- External collector only: rejected for this feature because users need a first-party local diagnostics story.
- Fold into Structured Logs: rejected because traces and metrics have different data shape, retention, and UI needs.

## Decision: Preserve existing workflow instrumentation as the producer

**Rationale**: `elsa-core` already emits workflow and activity spans/metrics through `Elsa.Workflows` `ActivitySource` and `Meter`. The diagnostics OpenTelemetry module should collect, display, and configure this telemetry rather than re-instrumenting workflow execution.

**Alternatives considered**:

- Add a second workflow instrumentation layer in the diagnostics module: rejected because duplicate spans would be easy to create and hard to reason about.
- Move instrumentation out of Workflows Core: rejected because instrumentation is useful even when the diagnostics collector is not installed.
- Port the historical `Elsa.OpenTelemetry` module from `elsa-extensions`: rejected for v1 because it is producer-side workflow/activity tracing middleware, not an OTLP collector or diagnostics backend. It also overlaps current Core instrumentation and contains trace-boundary behavior that mutates `Activity.Current`. Useful ideas such as custom error-span handlers and trace-boundary policy can be reconsidered later as focused producer instrumentation improvements.

## Decision: Support OTLP HTTP/protobuf first and gRPC through the same ingest contract

**Rationale**: HTTP/protobuf maps naturally to ASP.NET Core endpoints and standard `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`. gRPC remains important because many SDKs default to it. Both transports should feed one `IOpenTelemetryIngestor` so parsing, redaction, storage, and tests are shared.

**Alternatives considered**:

- gRPC only: rejected because HTTP/protobuf is easier to route through existing web hosts and proxies.
- HTTP only permanently: rejected because it would make common SDK defaults less ergonomic.

## Decision: Use bounded in-memory storage for v1

**Rationale**: The existing diagnostics modules use bounded buffers for local troubleshooting. OTEL data can be high volume; a bounded default protects development hosts and keeps the first slice focused.

**Alternatives considered**:

- Reuse structured-log SQLite persistence immediately: rejected because traces and metrics need different schemas and query patterns.
- Store only raw OTLP payloads: rejected because Studio would need server-side filtering and trace assembly anyway.

## Decision: Normalize telemetry into Elsa diagnostics read models

**Rationale**: OTLP protobuf is transport-oriented. Studio needs stable models for resources, traces, spans, metrics, logs, and storage diagnostics. Normalized models also isolate Studio from OTLP schema evolution.

**Alternatives considered**:

- Return OTLP JSON directly: rejected because every Studio view would need protocol-specific mapping logic.
- Invent UI-only DTOs in Studio: rejected because filtering, ordering, redaction, and trace assembly belong on the backend.

## Decision: Treat OTLP logs as correlation data, not Structured Logs replacement

**Rationale**: Elsa Structured Logs captures semantic `ILogger` records and already has filtering, storage, and live UI. OTLP logs may arrive from arbitrary resources and should be shown in OpenTelemetry context, especially trace detail, while trace/span IDs link users to Structured Logs when installed.

**Alternatives considered**:

- Merge OTLP logs into Structured Logs: rejected because source, schema, permission, retention, and redaction semantics differ.
- Omit OTLP logs completely: rejected because OpenTelemetry's signal model includes logs and they are useful inside trace context.

## Decision: Expose collector configuration metadata instead of AppHost-style injection

**Rationale**: Aspire AppHost can inject environment variables because it launches resources. Elsa Studio generally does not. The backend should expose endpoint/protocol/header metadata and documentation should provide copyable environment variable examples.

**Alternatives considered**:

- Build an Elsa launcher/control plane: rejected as far beyond diagnostics scope.
- Require manual documentation only: rejected because Studio can make the active endpoint and security requirements visible.

## Decision: Protect non-loopback OTLP ingestion explicitly

**Rationale**: OTLP ingestion can include sensitive application metadata and high-volume traffic. Development loopback defaults are acceptable; remote ingestion must require an API key/header or equivalent configured protection.

**Alternatives considered**:

- Leave OTLP endpoints unauthenticated everywhere: rejected as unsafe.
- Require full Studio user auth for OTLP senders: rejected because standard OpenTelemetry SDKs support headers more broadly than interactive auth.
