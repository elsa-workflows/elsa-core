# Product Requirements: Diagnostics OpenTelemetry

**Feature Branch**: `008-diagnostics-otel`  
**Created**: 2026-05-25  
**Status**: Draft  
**Input**: User description: "Design a PRD and technical plan for OTEL across elsa-core and elsa-studio, informed by Aspire's dashboard collector and UI architecture, now that diagnostics console logs and structured logs exist in both repositories."

## Clarifications

### Session 2026-05-25

- Q: Which side owns the OpenTelemetry read DTO contract? -> A: Core APIs are the source of truth; Studio mirrors normalized DTOs and MUST NOT parse OTLP payloads.
- Q: How should direct Studio routes behave when the Core feature is unavailable or unauthorized? -> A: Navigation is hidden when unavailable, while direct routes render unavailable or unauthorized states without probing optional endpoints first.
- Q: When may OpenTelemetry link to Console Logs? -> A: Only through public route/query contracts when a telemetry resource can be mapped to a compatible console source and time range.
- Q: How should Studio handle live telemetry growth? -> A: Studio keeps per-view live buffers capped, drops or prunes oldest visible items first, and shows overflow/drop indicators.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Collect OpenTelemetry from Elsa services (Priority: P1)

An operator enables OpenTelemetry diagnostics on an Elsa backend and sees telemetry from the Elsa server, including workflow and activity traces, metrics, resource identity, and correlated log signals.

**Why this priority**: The diagnostics trilogy is incomplete without a first-party OpenTelemetry boundary. Collection must work before Studio can provide useful trace or metric views.

**Independent Test**: Run an Elsa backend with the OpenTelemetry diagnostics feature enabled, execute a workflow with at least two activities, and verify recent traces, spans, resources, metrics, and trace IDs are available from the backend diagnostics API.

**Acceptance Scenarios**:

1. **Given** an Elsa backend has OpenTelemetry diagnostics enabled, **When** workflow execution emits spans and metrics, **Then** the backend stores recent telemetry with service name, service instance, trace ID, span ID, timing, status, and workflow metadata.
2. **Given** an OpenTelemetry SDK sends OTLP traces, metrics, or logs to the backend collector endpoint, **When** the payload is accepted, **Then** the backend exposes the telemetry through diagnostics read APIs without requiring Studio to parse OTLP payloads.
3. **Given** telemetry contains sensitive attributes matching configured redaction rules, **When** the backend stores or streams it, **Then** sensitive values are redacted before provider boundaries.

---

### User Story 2 - Investigate workflow traces in Studio (Priority: P1)

A developer opens Studio Diagnostics and inspects an OpenTelemetry trace as a waterfall that shows workflow execution, activity execution, status, duration, and links to related structured logs.

**Why this priority**: Trace investigation is the primary user value of OpenTelemetry for Elsa workflows and should connect directly to existing diagnostics surfaces.

**Independent Test**: Use mocked backend telemetry for a workflow trace with child activity spans, errors, and matching structured logs; verify Studio renders the trace list, trace detail waterfall, span details, and links filtered by trace/span ID.

**Acceptance Scenarios**:

1. **Given** the backend advertises the OpenTelemetry feature and permission, **When** Studio navigation loads, **Then** an OpenTelemetry entry appears under Diagnostics.
2. **Given** the backend does not advertise the OpenTelemetry feature, **When** Studio navigation loads, **Then** the OpenTelemetry entry is hidden and direct-route access renders a feature-unavailable state without unhandled endpoint failures.
3. **Given** a trace contains workflow and activity spans, **When** the user opens trace details, **Then** Studio shows a dense waterfall with duration, status, service, workflow identifiers, activity identifiers, and error markers.
4. **Given** a span has a trace ID and span ID also present in Structured Logs, **When** the user chooses the related logs action, **Then** Studio opens Structured Logs filtered to the trace or span.

---

### User Story 3 - Monitor operational metrics (Priority: P2)

An operator inspects recent OpenTelemetry metrics for workflows and activities, including started/completed/faulted counts and activity duration, grouped by service, workflow definition, tenant, and status.

**Why this priority**: Metrics complement trace-level debugging with aggregate operational signals and confirm whether instrumentation is being collected.

**Independent Test**: Feed metric points for multiple resources and workflows; verify Studio can list instruments, show recent values, filter by resource, and avoid high-cardinality layout failures.

**Acceptance Scenarios**:

1. **Given** the backend has received workflow metric points, **When** the user opens the Metrics view, **Then** Studio lists instruments, latest values, units, descriptions, and recent trend points.
2. **Given** metrics are emitted by multiple services, **When** the user filters by resource, **Then** only matching metric series are shown.
3. **Given** a metric has high-cardinality attributes, **When** Studio renders it, **Then** the view remains bounded and exposes an overflow or truncation indicator rather than growing without limit.

---

### User Story 4 - Configure external OTLP senders (Priority: P3)

A developer copies the backend's OTLP endpoint information and configures the Elsa server or adjacent services to export telemetry to the diagnostics collector during development.

**Why this priority**: Unlike Aspire AppHost, Elsa Studio does not own process launch for every resource. The product still needs a clear, low-friction setup path.

**Independent Test**: Open the OpenTelemetry setup view or documentation, copy the displayed OTEL environment variables, configure a sample sender, and verify the sender appears as a resource in Studio.

**Acceptance Scenarios**:

1. **Given** the backend collector is enabled, **When** Studio requests collector configuration, **Then** it displays the HTTP endpoint, any enabled gRPC endpoint, protocol values, required headers, and recommended development export intervals.
2. **Given** no collector endpoint is configured or the feature is disabled, **When** the user opens the setup view, **Then** Studio shows a clear unavailable state with no failing backend calls.
3. **Given** a non-.NET service sends telemetry with standard resource attributes, **When** telemetry is accepted, **Then** Studio identifies it by service name and instance ID.

### Edge Cases

- OTLP payloads contain unsupported future fields or partially populated resources.
- A sender uses HTTP/protobuf while another uses gRPC.
- The backend receives telemetry faster than Studio can render.
- Multiple services share the same `service.name` but different `service.instance.id`.
- A trace references structured logs that have already expired from their own buffer.
- Metrics contain high-cardinality attributes or many time series.
- The backend restarts and loses in-memory telemetry.
- OTLP ingestion is exposed beyond loopback without a configured API key.
- OpenTelemetry diagnostics is installed but Structured Logs or Console Logs is not installed.
- The user opens a direct `/diagnostics/opentelemetry` route without permission.

## Requirements *(mandatory)*

### Functional Requirements

**Core collection and storage**

- **FR-001**: `elsa-core` MUST introduce an opt-in diagnostics OpenTelemetry feature under the `Elsa.Diagnostics.OpenTelemetry` identity.
- **FR-002**: The feature MUST be separate from `Elsa.Diagnostics.StructuredLogs` and `Elsa.Diagnostics.ConsoleLogs`.
- **FR-003**: The feature MUST accept OTLP trace, metric, and log payloads from standard OpenTelemetry SDKs.
- **FR-004**: The feature MUST support HTTP/protobuf OTLP ingestion and SHOULD support gRPC OTLP ingestion when the host has gRPC support enabled; collector metadata MUST make the gRPC endpoint nullable or explicitly disabled when gRPC is unavailable.
- **FR-005**: The backend MUST normalize incoming telemetry into bounded queryable models for resources, traces, spans, metrics, and OTLP log records.
- **FR-006**: The default store MUST be bounded in memory and MUST report dropped telemetry counts when capacity is exceeded.
- **FR-007**: Telemetry redaction MUST run before provider storage or live streaming.
- **FR-008**: Resource identity MUST use OpenTelemetry resource attributes, including service name and service instance ID when present.
- **FR-009**: Workflow and activity telemetry MUST preserve Elsa workflow identifiers, activity identifiers, status, duration, tenant ID, and fault information when emitted by existing Elsa instrumentation.
- **FR-010**: The feature MUST expose source/resource listing, recent trace search, trace detail, metric search, OTLP log search, storage diagnostics, and collector configuration through authenticated diagnostics APIs.
- **FR-011**: The feature MUST expose live updates through an authenticated SignalR hub or equivalent existing real-time pattern.
- **FR-012**: The backend MUST enforce a diagnostics OpenTelemetry view permission for all Studio-facing APIs and live connections.
- **FR-013**: OTLP ingestion MUST be safe by default: loopback-only development mode is acceptable without a shared secret, but non-loopback ingestion MUST require an explicit API key or equivalent configured protection.

**Studio visualization**

- **FR-014**: `elsa-studio` MUST introduce a focused `Elsa.Studio.Diagnostics.OpenTelemetry` module.
- **FR-015**: The Studio module MUST gate navigation and direct routes on the remote `Elsa.Diagnostics.OpenTelemetry` feature and required permission; navigation is hidden when the feature is unavailable, and direct routes render explicit unavailable or unauthorized states before calling optional OpenTelemetry endpoints.
- **FR-016**: The canonical Studio route MUST be `/diagnostics/opentelemetry`.
- **FR-017**: Studio MUST provide dense operational views for resources, traces, trace detail, metrics, OTLP logs, and collector setup using normalized backend DTOs rather than raw OTLP/protobuf parsing.
- **FR-018**: Trace detail MUST render parent/child spans as a waterfall or equivalent timing view with status, duration, service, and key Elsa workflow attributes.
- **FR-019**: Studio MUST preserve useful filters in URL query state, including resource, trace ID, span ID, service name, workflow instance ID, workflow definition ID, severity, status, text, and time range.
- **FR-020**: Studio MUST link trace/span context to Structured Logs when that module is available.
- **FR-021**: Studio SHOULD link resource/time context to Console Logs when that module is available and the OpenTelemetry resource can be mapped to a compatible console-log source through public route/query contracts.
- **FR-022**: Studio MUST show distinct loading, empty, feature-unavailable, unauthorized, disconnected, reconnecting, error, and storage-overflow states.
- **FR-023**: Studio MUST cap or virtualize visible rows and series so live telemetry cannot grow UI memory without bounds; when a per-view cap is exceeded, Studio MUST prune or drop the oldest visible live items first and expose an overflow/drop indicator.

**Configuration and interoperability**

- **FR-024**: The backend MUST expose collector endpoint metadata suitable for standard OTEL environment variables.
- **FR-025**: The feature documentation MUST show .NET setup using the existing `Elsa.Workflows` activity source and meter.
- **FR-026**: The feature documentation MUST show polyglot OTLP configuration using standard `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_SERVICE_NAME`, and `OTEL_RESOURCE_ATTRIBUTES` variables.
- **FR-027**: The feature MUST document that production deployments should usually export to an external collector or observability backend, with the Elsa collector intended for development and focused diagnostics unless capacity is configured deliberately.
- **FR-028**: The feature MUST avoid duplicating Structured Logs and Console Logs behavior; it may correlate to those modules but must not replace their contracts.

### Key Entities

- **Telemetry Resource**: A service/process/resource identified by OpenTelemetry resource attributes.
- **Trace**: A distributed operation grouped by trace ID and composed of spans.
- **Span**: A timed operation with parent/child relationships, status, attributes, events, and links.
- **Metric Instrument**: A named OpenTelemetry metric with unit, description, temporality, aggregation, attributes, and recent data points.
- **OTLP Log Record**: A log record received through OTLP, separate from Elsa's first-party Structured Logs module but correlated by trace/span IDs when available.
- **Collector Configuration**: Endpoint, protocol, header, and environment-variable metadata used by telemetry senders.
- **Telemetry Subscription**: A live Studio connection scoped by filters and connection state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A local Elsa backend with OpenTelemetry diagnostics enabled receives workflow trace spans and exposes them through the diagnostics API within 2 seconds of workflow execution.
- **SC-002**: Studio opens `/diagnostics/opentelemetry` against an enabled backend and displays recent traces plus live connection state within 2 seconds.
- **SC-003**: A trace detail view with at least 100 spans remains responsive and does not overflow its layout at common desktop widths.
- **SC-004**: Recent metric views render at least 20 instruments and 1,000 recent points while keeping client-side memory bounded by configured caps.
- **SC-005**: API and SignalR calls return unauthorized/unavailable states instead of unhandled failures when the feature or permission is missing.
- **SC-006**: Sensitive configured attribute names and text patterns are redacted before stored telemetry is returned to Studio.
- **SC-007**: Documentation includes working .NET and non-.NET OTLP configuration examples and clearly separates OpenTelemetry from Structured Logs and Console Logs.

## Assumptions

- Existing Elsa workflow instrumentation through `Elsa.Workflows` `ActivitySource` and `Meter` remains the producer of workflow spans and metrics.
- The historical `Elsa.OpenTelemetry` module in `elsa-extensions` is not the implementation basis for this feature; useful producer-side ideas from it are deferred.
- The first collector store is bounded in memory; durable OpenTelemetry persistence is a later provider feature.
- Studio and backend API contracts use the same authentication and remote-feature patterns as the structured and console diagnostics modules.
- Studio cannot inject environment variables into arbitrary processes the way Aspire AppHost can, so this feature exposes configuration metadata and documentation instead of owning external process launch.
- The first version optimizes for local development and focused troubleshooting, not replacing production observability platforms.
