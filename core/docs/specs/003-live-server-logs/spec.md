# Feature Specification: Live Server Log Streaming

**Feature Branch**: `003-live-server-logs`  
**Created**: 2026-05-06  
**Status**: Draft  
**Input**: User description: "Add a module / integration to Elsa Studio that lets users see console output from the server backend, like Aspire's dashboard. Consider clustered hosting such as Kubernetes, with a merged view and individual pod views."

## Clarifications

### Session 2026-05-06

- Q: Should the feature mirror raw `Console.Out` or structured logs? -> A: Capture structured `ILogger` events and render them console-style. Raw console capture is not the primary contract.
- Q: Should clustered deployments show one merged stream or pod-specific streams? -> A: Both. The merged stream is the default; source-specific filtering is a first-class capability.
- Q: Is this durable audit logging? -> A: No. It is live server log streaming with bounded recent history. Durable retention belongs to existing observability systems.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tail live server logs from Studio (Priority: P1)

An administrator or developer opens Studio and watches live backend log events without shelling into the server process.

**Why this priority**: This is the core capability and delivers immediate value for local development and single-node deployments.

**Independent Test**: Enable the backend feature, connect a SignalR client, emit `ILogger` messages at multiple levels, and verify recent backfill plus live events are delivered in order with structured fields.

**Acceptance Scenarios**:

1. **Given** log streaming is enabled and the caller is authorized, **When** the caller requests recent log events, **Then** the server returns a bounded ordered slice of recent log events.
2. **Given** a caller is subscribed to the live stream, **When** the backend writes an `ILogger` event, **Then** the caller receives the event with timestamp, level, category, message, exception details if present, and source metadata.
3. **Given** the live event buffer reaches its configured capacity, **When** new log events arrive, **Then** the oldest events are discarded and the server reports dropped-event counts to subscribers.

---

### User Story 2 - Filter and secure operational logs (Priority: P2)

An operator narrows the stream to warning/error logs, a logger category, a tenant, a workflow instance, or a source process while the backend enforces authorization and redaction.

**Why this priority**: Logs often contain sensitive information and can be noisy. The feature is not safe or useful without filtering, permissions, and redaction.

**Independent Test**: Connect with and without `read:server-logs`, emit events with sensitive-looking properties and multiple scopes, and verify unauthorized callers are rejected while authorized callers receive redacted and filter-matched events only.

**Acceptance Scenarios**:

1. **Given** an unauthenticated or unauthorized caller, **When** the caller uses the recent-log endpoint or SignalR hub, **Then** the request is rejected.
2. **Given** redaction rules are configured, **When** log messages, exception details, scopes, or properties contain matching sensitive values, **Then** the streamed event replaces those values with a redaction marker.
3. **Given** a caller supplies level, category, tenant, workflow instance, correlation, or source filters, **When** matching and non-matching events are emitted, **Then** only matching events are returned or streamed.

---

### User Story 3 - Inspect clustered log sources (Priority: P3)

An operator running Elsa in Kubernetes or another clustered environment views a merged stream across backend replicas and can switch to one pod, container, or process source when diagnosing a specific replica.

**Why this priority**: Clustered deployments are common for production. Source identity must be present from day one so the feature does not become single-process-only.

**Independent Test**: Configure multiple simulated log sources through the provider abstraction and verify that the API lists sources, streams a merged view, and filters to an individual source without changing the Studio contract.

**Acceptance Scenarios**:

1. **Given** multiple Elsa processes publish log events through a configured provider, **When** the caller subscribes without a source filter, **Then** the server streams a merged ordered view with each event's source identity.
2. **Given** the caller filters by a source ID, **When** events arrive from multiple sources, **Then** only events from the selected source are streamed.
3. **Given** a source stops publishing heartbeats, **When** the caller lists sources, **Then** the source is marked stale or disconnected without deleting its recent log history immediately.

### Edge Cases

- The SignalR client disconnects while events continue to arrive.
- The log provider emits events faster than clients can consume them.
- System clocks differ between cluster nodes.
- A log message contains secrets in formatted message text, scope values, exception data, or structured properties.
- A caller changes filters while subscribed.
- A source identifier changes after a pod restart.
- The configured shared provider is unavailable at startup or becomes unavailable during streaming.
- Logging the log-streaming feature itself creates feedback loops.

## Requirements *(mandatory)*

### Functional Requirements

**Capture and buffering**

- **FR-001**: The backend MUST provide an opt-in Elsa feature that registers a structured `ILoggerProvider` for log streaming.
- **FR-002**: The feature MUST capture timestamp, level, category, event ID, rendered message, exception summary, exception detail, scopes, structured properties, trace ID, span ID, correlation ID, tenant ID, workflow definition ID, workflow instance ID, and source ID when available.
- **FR-003**: The feature MUST keep a bounded recent-history buffer with configurable capacity and MUST never grow memory without bound.
- **FR-004**: The feature MUST track dropped-event counts when buffer or channel capacity is exceeded.
- **FR-005**: The feature MUST avoid recursively streaming its own internal server log module logs unless explicitly enabled for troubleshooting.

**Streaming and query surface**

- **FR-006**: The backend MUST expose an authenticated SignalR hub for live log subscriptions.
- **FR-007**: The backend MUST expose an authenticated recent-log endpoint for initial backfill.
- **FR-008**: The backend MUST expose an authenticated source-list endpoint that returns known log sources and their health state.
- **FR-009**: The hub and endpoints MUST support filters for minimum level, exact levels, category prefix, free-text query, tenant ID, workflow definition ID, workflow instance ID, trace ID, correlation ID, source ID, and time range.
- **FR-010**: The hub MUST allow a subscriber to update filters without opening a new connection.
- **FR-011**: The recent-log endpoint MUST cap maximum `take` values server-side regardless of client input.

**Authorization and safety**

- **FR-012**: All log-streaming endpoints and hubs MUST require authorization using a dedicated permission, `read:server-logs`.
- **FR-013**: The feature MUST support configurable redaction rules that apply to messages, exception text, scopes, and structured properties before events leave the server.
- **FR-014**: The feature MUST provide conservative default redaction for common secret names such as authorization, token, password, secret, api-key, cookie, and connection-string.
- **FR-015**: Redaction MUST run before buffering when using any provider that can expose buffered events to callers.

**Cluster and source topology**

- **FR-016**: Every streamed event MUST include a stable source descriptor identifying at least source ID, display name, service name, process ID, machine name, and, when available, pod name, container name, namespace, and node name.
- **FR-017**: The in-memory provider MUST support single-process development and tests.
- **FR-018**: The design MUST define a provider abstraction that allows cluster-capable providers such as Redis, OpenTelemetry, Loki, Elasticsearch, Seq, or Application Insights without changing Studio contracts.
- **FR-019**: Cluster-capable providers MUST preserve source identity and SHOULD expose source health through heartbeat or last-seen timestamps.
- **FR-020**: The merged stream MUST tolerate moderate timestamp skew by using server receive order as a deterministic tiebreaker.

**Configuration and integration**

- **FR-021**: The feature MUST be registered via a fluent Elsa module extension such as `UseServerLogStreaming`.
- **FR-022**: The feature MUST appear in the installed-feature list so Studio can hide or show the UI based on backend capability.
- **FR-023**: The feature MUST document middleware requirements, including SignalR hub mapping.
- **FR-024**: The feature MUST provide options for buffer capacity, channel capacity, maximum recent-log query size, source heartbeat timeout, redaction rules, and provider selection.
- **FR-025**: The feature MUST work with Elsa's existing authentication and CORS patterns.

### Key Entities

- **Log Event**: A structured server log event captured from `ILogger`.
- **Log Source**: The process, pod, container, or external provider source that produced a log event.
- **Log Subscription**: A SignalR connection with mutable filters and backpressure state.
- **Recent Log Buffer**: Bounded storage of redacted recent events used for initial backfill.
- **Log Streaming Provider**: Pluggable implementation that stores and distributes events in-process or through a shared backend.
- **Redaction Rule**: A configured pattern or property-name rule applied before events leave the backend.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In single-process validation, an authorized subscriber receives 100% of emitted test events up to configured channel capacity within 1 second.
- **SC-002**: Recent-log queries never return more than the configured maximum number of events.
- **SC-003**: Unauthorized callers are rejected for 100% of hub, recent-log, and source-list access attempts.
- **SC-004**: Default redaction masks common secret property names in messages, scopes, and structured properties in validation tests.
- **SC-005**: In a simulated three-source cluster, the merged stream includes events from all sources and source filtering returns only the selected source.
- **SC-006**: A source that stops heartbeating is marked stale within the configured heartbeat timeout.
- **SC-007**: Under sustained overload, memory remains bounded and dropped-event counts are visible to subscribers.

## Assumptions

- Studio will implement a separate paired spec under the same feature ID.
- SignalR is the preferred live transport because Studio already has authentication hooks for SignalR connections.
- Shared cluster aggregation is provider-driven; Kubernetes API access is not required for the MVP.
- Log events are operational server telemetry, not compliance/audit records.
- Existing external observability products remain the long-term retention mechanism.
