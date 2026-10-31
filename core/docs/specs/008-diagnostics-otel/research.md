# Research: Diagnostics OpenTelemetry

## Decision: Core owns the collector/API boundary

**Rationale**: OTLP ingestion must happen in an ASP.NET Core backend, not in Studio. Core can authenticate diagnostics APIs, protect ingestion, normalize protobuf payloads, and stream live updates.

**Alternatives considered**:

- Studio collector: rejected because browser/Studio hosts cannot reliably expose OTLP endpoints.
- External collector only: rejected because this feature needs a first-party local diagnostics story.

## Decision: Preserve existing workflow instrumentation

**Rationale**: Current `Elsa.Workflows.Core` already emits `Elsa.Workflows` spans and metrics through `System.Diagnostics`. This module should consume/export that telemetry through OTLP rather than re-instrument workflow execution.

**Alternatives considered**:

- Port `elsa-extensions` `Elsa.OpenTelemetry`: rejected because it is producer-side middleware, overlaps current Core instrumentation, and includes trace-boundary behavior that mutates ambient `Activity.Current`.
- Move instrumentation into diagnostics: rejected because workflow instrumentation is valuable even when diagnostics collector is not installed.

## Decision: HTTP/protobuf required, gRPC optional

**Rationale**: HTTP/protobuf fits existing ASP.NET Core endpoint patterns and common deployment routes. gRPC is useful for SDK defaults but depends on host configuration and package choices, so it should be enabled when available and represented honestly in collector metadata.

**Alternatives considered**:

- gRPC only: rejected because it complicates proxying and local HTTP testing.
- Always return a gRPC endpoint: rejected because it misleads clients when the host does not expose gRPC.

## Decision: Bounded in-memory v1 storage

**Rationale**: Traces, metrics, and logs can be high volume. A bounded in-memory default matches the existing diagnostics modules and keeps v1 focused.

**Alternatives considered**:

- Durable storage now: rejected because trace and metric schemas need a deliberate follow-up provider design.
- Raw OTLP payload storage only: rejected because diagnostics clients need filtered, assembled trace and metric models.

## Decision: OTLP logs stay separate from Structured Logs

**Rationale**: OTLP logs can come from arbitrary resources and use OTEL schemas. `Elsa.Diagnostics.StructuredLogs` owns first-party `ILogger` capture. Correlation should use trace/span IDs, not shared storage.

**Alternatives considered**:

- Merge OTLP logs into Structured Logs: rejected because schemas, retention, permissions, and source semantics differ.

## Decision: Explicit non-loopback protection

**Rationale**: OTLP ingestion can expose sensitive application metadata and create high traffic. Loopback development can be low friction, but remote ingestion needs an API key header or equivalent configured protection.

**Alternatives considered**:

- No OTLP sender auth: rejected as unsafe.
- Require interactive user auth for OTLP senders: rejected because SDKs usually support headers, not browser auth.
