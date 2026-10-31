# Feature Specification: Diagnostics OpenTelemetry

**Feature Branch**: `008-diagnostics-otel`  
**Created**: 2026-05-25  
**Status**: Draft  
**Input**: Core-owned specification for the backend side of the OpenTelemetry diagnostics PRD coordinated with `elsa-studio/specs/008-diagnostics-otel`.

## Clarifications

### Session 2026-05-25

- Q: What default capacity and overflow policy should the Core in-memory OTEL store use? -> A: Configurable bounded defaults with drop-oldest per signal.
- Q: How should Core decide when OTLP ingestion requires an API key? -> A: Require the configured header for any non-loopback request or non-loopback collector binding.
- Q: How should live SignalR subscriptions behave under backpressure? -> A: Use bounded per-connection queues, drop oldest updates, and report dropped counts.
- Q: Which workflow attributes are canonical for Elsa trace correlation? -> A: Preserve existing `Elsa.Workflows` semantic tags; do not add producer middleware or mutate ambient activity context.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Collect OpenTelemetry from Elsa services (Priority: P1)

An operator enables OpenTelemetry diagnostics on an Elsa backend and receives workflow traces, activity spans, metrics, resources, and correlated OTLP logs through a first-party diagnostics backend.

**Why this priority**: Studio cannot visualize telemetry until Core can collect, normalize, secure, and expose it.

**Independent Test**: Enable the Core OpenTelemetry diagnostics feature, run a workflow with multiple activities, export telemetry to the local diagnostics collector, and verify recent resources, traces, spans, metrics, and logs through backend diagnostics APIs.

**Acceptance Scenarios**:

1. **Given** the diagnostics OpenTelemetry feature is enabled, **When** workflow execution emits spans and metrics from `Elsa.Workflows`, **Then** Core stores recent telemetry with resource identity, trace/span IDs, timing, status, and workflow metadata.
2. **Given** a standard OpenTelemetry SDK posts OTLP traces, metrics, or logs to the HTTP/protobuf collector endpoint, **When** the payload is accepted, **Then** Core normalizes the telemetry into queryable diagnostics models.
3. **Given** telemetry contains sensitive attributes matching configured redaction rules, **When** Core stores or streams it, **Then** sensitive values are redacted before provider boundaries.

---

### User Story 2 - Serve Trace Investigation APIs (Priority: P1)

Studio and other authenticated diagnostics clients can query recent traces and retrieve a trace detail model with ordered spans, resource metadata, OTLP logs, and storage diagnostics.

**Why this priority**: Trace investigation is the main workflow troubleshooting path and must be backend-filtered before Studio renders a waterfall.

**Independent Test**: Seed or ingest telemetry for a workflow trace and call the trace search/detail APIs with filters for service, resource, trace ID, workflow instance ID, status, text, and time range.

**Acceptance Scenarios**:

1. **Given** a trace contains workflow and activity spans, **When** a diagnostics client requests trace detail, **Then** Core returns parent/child span data ordered for a waterfall view.
2. **Given** filters include workflow instance ID or trace ID, **When** trace search is executed, **Then** only matching trace summaries are returned.
3. **Given** the caller lacks the OpenTelemetry diagnostics view permission, **When** the caller requests trace APIs or live updates, **Then** Core denies the request without exposing telemetry.

---

### User Story 3 - Serve Metrics and OTLP Logs (Priority: P2)

An authenticated diagnostics client can inspect recent OpenTelemetry metric instruments, bounded metric points, OTLP log records, and overflow diagnostics.

**Why this priority**: Metrics and OTLP logs complement traces and validate that all OpenTelemetry signals are flowing.

**Independent Test**: Ingest metrics and OTLP logs for multiple resources, query them by resource, instrument, severity, trace/span ID, text, and time range, and verify capacity diagnostics.

**Acceptance Scenarios**:

1. **Given** metrics are emitted by multiple services, **When** a client filters by resource or instrument, **Then** Core returns only matching bounded series.
2. **Given** a metric has high-cardinality attributes, **When** capacity is exceeded, **Then** Core drops according to configured policy and reports dropped point counts.
3. **Given** OTLP logs include trace/span IDs, **When** logs are queried by trace/span, **Then** Core returns correlated OTLP log records without merging them into `Elsa.Diagnostics.StructuredLogs`.

---

### User Story 4 - Expose Collector Configuration and Secure Ingestion (Priority: P3)

A developer can discover active collector endpoints and configure .NET or non-.NET senders using standard OTEL environment variables, while Core protects non-loopback ingestion.

**Why this priority**: Elsa does not own an Aspire-style launcher, so Core must make active collector configuration discoverable and secure.

**Independent Test**: Request collector configuration, verify HTTP metadata and nullable/disabled gRPC metadata, configure a sample sender, and verify non-loopback ingestion requires the configured API key header.

**Acceptance Scenarios**:

1. **Given** the collector is enabled, **When** a diagnostics client requests collector configuration, **Then** Core returns HTTP endpoint metadata, any enabled gRPC endpoint metadata, required header names, and recommended non-secret environment variables.
2. **Given** gRPC ingestion is unavailable, **When** configuration is requested, **Then** Core marks gRPC disabled instead of returning a misleading endpoint.
3. **Given** OTLP ingestion is exposed beyond loopback, **When** a sender omits the required API key header, **Then** Core rejects the request.

### Edge Cases

- OTLP payloads include unsupported future fields or incomplete resource attributes.
- Multiple services share `service.name` but have different `service.instance.id` values.
- A trace arrives out of span order.
- Metrics create more series or points than configured capacity allows.
- Backend restarts and loses in-memory telemetry.
- OTLP logs reference trace/span IDs for traces that have expired.
- gRPC support is not available in the hosting configuration.
- The caller has Studio access but not the OpenTelemetry diagnostics permission.
- A sender posts from loopback while the collector is configured for loopback-only development.
- A sender posts from a non-loopback address without the configured ingestion API key header.
- A live client subscribes with filters that receive more updates than its per-connection queue allows.
- The historical `Elsa.OpenTelemetry` extension package is present in another repository but must not be ported into this module.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Core MUST introduce an opt-in module under `src/modules/Elsa.Diagnostics.OpenTelemetry`.
- **FR-002**: The module MUST be separate from `Elsa.Diagnostics.StructuredLogs` and `Elsa.Diagnostics.ConsoleLogs`.
- **FR-003**: The module MUST use existing `Elsa.Workflows` `ActivitySource` and `Meter` instrumentation as the producer of Elsa workflow telemetry.
- **FR-004**: The module MUST NOT port the historical `Elsa.OpenTelemetry` producer-side middleware from `elsa-extensions` in v1.
- **FR-005**: The module MUST accept OTLP HTTP/protobuf traces, metrics, and logs.
- **FR-006**: The module SHOULD support OTLP gRPC ingestion when the host has gRPC support enabled; collector metadata MUST represent gRPC as disabled or null when unavailable.
- **FR-007**: HTTP and gRPC ingestion, when both are enabled, MUST feed one shared ingestion contract for normalization, redaction, storage, and live publishing.
- **FR-008**: The module MUST normalize telemetry into bounded queryable models for resources, traces, spans, metrics, and OTLP log records.
- **FR-009**: The default store MUST be bounded in memory and MUST report dropped telemetry counts.
- **FR-010**: Redaction MUST run before telemetry reaches provider storage or live subscribers.
- **FR-011**: Resource identity MUST use OpenTelemetry resource attributes, including service name and service instance ID when present.
- **FR-012**: Workflow and activity telemetry MUST preserve the existing `Elsa.Workflows` semantic attributes when present, including `workflow.instance.id`, `workflow.definition.id`, `workflow.definition.version`, `workflow.definition.version.id`, `workflow.status`, `workflow.substatus`, `workflow.faulted`, `workflow.parent.instance.id`, `workflow.correlation.id`, `workflow.activity.id`, `workflow.activity.name`, `workflow.activity.type`, `workflow.activity.version`, `workflow.activity.execution.id`, `workflow.activity.status`, `workflow.activity.outcome`, `workflow.activity.parent.execution.id`, `workflow.activity.scheduled.by.execution.id`, `workflow.activity.faulted`, `elsa.tenant.id`, and `exception.type`.
- **FR-013**: The module MUST expose authenticated diagnostics APIs for resources, trace search, trace detail, metrics, OTLP logs, storage diagnostics, and collector configuration.
- **FR-014**: The module MUST expose live updates through an authenticated SignalR hub.
- **FR-015**: The module MUST enforce an OpenTelemetry diagnostics view permission for all diagnostics APIs and live connections.
- **FR-016**: Loopback-only development ingestion MAY run without an API key only for loopback requests while the collector is bound to loopback; any non-loopback request or non-loopback collector binding MUST require an explicit API key header or equivalent configured protection.
- **FR-017**: Collector configuration MUST expose standard OTEL environment variable metadata without exposing secret values.
- **FR-018**: The module MUST document that production deployments should generally export to an external OpenTelemetry Collector or observability backend unless Elsa collector capacity and security are deliberately configured.
- **FR-019**: OTLP logs MUST remain separate from `Elsa.Diagnostics.StructuredLogs`; correlation uses trace/span IDs rather than shared storage.
- **FR-020**: The module MUST include tests for ingestion, normalization, redaction, bounded storage, permissions, live updates, collector configuration, and end-to-end workflow export-to-collector timing.
- **FR-021**: In-memory storage defaults MUST be configurable and MUST start with bounded development defaults of at least 500 resources, 2,000 traces, 10,000 spans, 20,000 metric points, 10,000 OTLP log records, and 1,000 queued live updates per subscriber; when a capacity is exceeded, the oldest item in that signal-specific buffer is dropped and the relevant dropped count is incremented.
- **FR-022**: Search APIs MUST return deterministic, bounded result sets with caller-specified limits capped by server options; default ordering is newest receive time first for searches and parent/child chronological ordering for trace detail spans.
- **FR-023**: Live SignalR subscriptions MUST use bounded per-connection queues, update filters in place, drop oldest queued updates on overflow, and publish dropped-update diagnostics without disconnecting healthy subscribers.
- **FR-024**: The diagnostics module MUST NOT start new workflow or activity spans itself and MUST NOT mutate `Activity.Current`; trace production remains owned by existing `Elsa.Workflows.Core` instrumentation.

### Key Entities

- **Telemetry Resource**: Resource/service identity derived from OTEL resource attributes.
- **Telemetry Trace**: Trace summary grouped by trace ID.
- **Telemetry Span**: Timed operation inside a trace with attributes, events, links, and status.
- **Metric Instrument**: Metric name, unit, description, type, resource, and bounded series.
- **Metric Point**: Recent data point for a metric series.
- **OTLP Log Record**: Log record received through OTLP and correlated by trace/span IDs.
- **Collector Configuration**: Active endpoint, protocol, security, and environment-variable metadata.
- **Storage Diagnostics**: Capacity and dropped telemetry counters.
- **Telemetry Subscription**: Live SignalR subscription with filters and connection state.
- **Storage Capacity Policy**: Configurable per-signal limits and drop-oldest overflow behavior for in-memory telemetry.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A local Core host with OpenTelemetry diagnostics enabled receives workflow trace spans and exposes them through diagnostics APIs within 2 seconds of workflow execution.
- **SC-002**: Core accepts representative OTLP HTTP/protobuf traces, metrics, and logs from standard SDK payloads and returns normalized diagnostics models.
- **SC-003**: Trace detail APIs return at least 100 ordered spans for one trace without unbounded memory growth.
- **SC-004**: Metric APIs return at least 20 instruments and 1,000 recent points while honoring configured capacity.
- **SC-005**: Unauthorized API and hub calls are denied without exposing telemetry.
- **SC-006**: Sensitive configured attribute names and text patterns are redacted before stored telemetry is returned.
- **SC-007**: Collector configuration reports HTTP metadata, nullable/disabled gRPC metadata, and required header names accurately.
- **SC-008**: Capacity tests prove each signal-specific buffer drops oldest telemetry, increments dropped counts, and keeps queries bounded when defaults are exceeded.
- **SC-009**: SignalR tests prove subscriber overflow drops oldest queued live updates, reports dropped-update counts, and keeps the connection usable.

## Assumptions

- `Elsa.Workflows.Core` keeps first-party workflow tracing and metrics instrumentation.
- Durable OpenTelemetry persistence is a later provider feature.
- The Studio feature has its own spec in `elsa-studio/specs/008-diagnostics-otel`.
- Core and Studio contracts should remain aligned, but Core is the source of truth for ingestion, permissions, normalization, and API behavior.
- Default in-memory capacities are development-friendly starting points, not production sizing guidance.
