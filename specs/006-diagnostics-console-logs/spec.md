# Feature Specification: Diagnostics Console Logs

**Feature Branch**: `006-diagnostics-console-logs`  
**Created**: 2026-05-18  
**Status**: Draft  
**Input**: User description: "Create a Core feature spec for diagnostics console streaming based on the existing roadmap at specs/diagnostics-console-streaming-roadmap.md and the surrounding context from specs/003-live-server-logs, specs/004-diagnostics-structured-logs, and specs/005-structured-log-persistence. Include requirements for backend capture, buffering, endpoints, SignalR hub, permissions, source identity, redaction, and provider boundaries. Avoid implementation code changes."

## Clarifications

### Session 2026-05-18

- Q: How should Core expose partial stdout/stderr writes that have not ended with a newline? -> A: Buffer until complete.
- Q: How should Core handle console lines longer than the configured maximum line length? -> A: Truncate oversized lines.
- Q: What should Core do with ANSI escape sequences by default? -> A: Strip ANSI by default.
- Q: What permission boundary should Core use for source listings? -> A: Same dedicated permission.
- Q: What content should console log providers receive and retain? -> A: Redacted content only.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tail raw backend console output (Priority: P1)

An administrator or developer enables the Core console logs feature and watches recent plus live stdout and stderr lines from the backend process without using shell access.

**Why this priority**: Raw process console output is the core capability and is intentionally separate from structured log records.

**Independent Test**: Enable the console logs feature, write distinct lines to stdout and stderr, request recent lines, subscribe to the live stream, and verify callers receive ordered, source-aware console line events while the original console output still reaches its normal destination.

**Acceptance Scenarios**:

1. **Given** diagnostics console logs are enabled and the caller is authorized, **When** the backend writes complete lines to stdout and stderr, **Then** the caller receives console line events with stream identity, text, timestamps, sequence, and source identity.
2. **Given** console capture is enabled, **When** the backend writes to stdout or stderr, **Then** existing console behavior is preserved and output remains visible to the host environment.
3. **Given** the caller requests recent console lines before subscribing live, **When** recent matching lines exist, **Then** the server returns a bounded ordered backfill before the caller receives new live lines.

---

### User Story 2 - Filter, secure, and redact console output (Priority: P2)

An operator narrows noisy console output by source, stream, text, and time while the backend enforces a dedicated diagnostics permission and redacts sensitive data before data leaves the backend.

**Why this priority**: Console output can contain secrets and high-volume operational noise. The feature is not safe for real hosts without authorization, filtering, and redaction.

**Independent Test**: Connect authorized and unauthorized callers, write console lines containing secret-like values, apply filters, and verify unauthorized access is rejected while authorized callers only receive redacted lines matching their filters.

**Acceptance Scenarios**:

1. **Given** an unauthenticated or unauthorized caller, **When** the caller requests recent console lines, source listings, or a live subscription, **Then** access is rejected.
2. **Given** redaction rules are configured, **When** stdout or stderr contains matching sensitive values, **Then** recent and live events replace those values with a redaction marker before exposure.
3. **Given** the caller filters by source, stream, text, or time, **When** matching and non-matching console lines are available, **Then** only matching redacted lines are returned or streamed.

---

### User Story 3 - Identify console sources in clustered deployments (Priority: P3)

An operator diagnosing a clustered Elsa deployment can view a merged stream across known backend sources and filter to one source such as a process, pod, or container.

**Why this priority**: Source identity must be part of the first contract so the console viewer does not become single-process-only and can later use shared providers without changing Studio-facing behavior.

**Independent Test**: Simulate multiple console log sources through the provider boundary, request sources, subscribe to a merged stream, and then filter to one source while preserving source health and dropped-line metadata.

**Acceptance Scenarios**:

1. **Given** multiple sources publish console lines through the configured provider, **When** the caller subscribes without a source filter, **Then** the server streams a merged ordered view with each line's source identity.
2. **Given** the caller filters by a source ID, **When** lines arrive from multiple sources, **Then** only lines from the selected source are returned or streamed.
3. **Given** a source stops sending heartbeats or lines, **When** the caller lists sources, **Then** the source is marked stale or disconnected without immediately losing its recent history.

### Edge Cases

- Console writes arrive as partial lines or without a trailing newline.
- A console line exceeds the configured maximum line length.
- Output arrives faster than the recent buffer or subscriber queues can handle.
- A subscriber disconnects while console lines continue to arrive.
- A caller changes filters while subscribed.
- Console output contains ANSI escape sequences.
- Console output contains secrets in raw text or source metadata.
- Multiple sources have clock skew or overlapping sequence values.
- The console logs feature writes its own diagnostics messages and risks feeding them back into the captured console stream.
- The configured provider is unavailable at startup or becomes unavailable during streaming.

## Requirements *(mandatory)*

### Functional Requirements

**Module boundary and feature identity**

- **FR-001**: The backend MUST provide an opt-in Core diagnostics module named `Elsa.Diagnostics.ConsoleLogs` or an equivalent diagnostics console logs name confirmed before implementation.
- **FR-002**: The console logs module MUST be separate from `Elsa.Diagnostics.StructuredLogs` and MUST NOT replace, rename, or parse structured log records.
- **FR-003**: The module MUST advertise a diagnostics console logs remote feature so Studio can detect whether the backend supports console streaming.
- **FR-004**: The feature MUST document that raw stdout/stderr console streaming is distinct from structured logs, structured log persistence, trace waterfalls, metrics, and OpenTelemetry exploration.

**Backend capture and line behavior**

- **FR-005**: The feature MUST capture writes to stdout and stderr as raw console output while preserving the host's existing console output behavior.
- **FR-006**: Captured output MUST be emitted as line-oriented console events with stream identity of `stdout` or `stderr`.
- **FR-007**: Captured console events MUST include ID, timestamp, received timestamp, source-local sequence, stream, text, source identity, truncation indicator, and dropped-line metadata when available.
- **FR-008**: Partial writes MUST be buffered and MUST NOT be exposed as separate fragment events; Core completes and emits a line only on newline, maximum line length, or configurable idle flush timeout.
- **FR-009**: Lines longer than the configured maximum line length MUST be truncated to the configured maximum, emitted as a single console line event, and marked with a truncation indicator.
- **FR-010**: ANSI escape sequences MUST be stripped by default before exposure, and hosts MUST be able to configure preservation of terminal formatting when needed.
- **FR-011**: The feature MUST avoid recursively capturing console diagnostics generated by the console logs feature itself unless explicitly enabled for troubleshooting.

**Buffering and backpressure**

- **FR-012**: The feature MUST keep a bounded recent-history buffer for initial backfill and MUST never grow memory without bound.
- **FR-013**: The feature MUST use bounded live subscriber behavior and MUST track dropped-line counts when buffers or subscriber queues overflow.
- **FR-014**: Recent-line requests MUST enforce a server-side maximum result count regardless of caller input.
- **FR-015**: Dropped-line summaries MUST identify the affected source and reason when that information is known.

**Endpoints and live transport**

- **FR-016**: The backend MUST expose an authenticated recent console lines endpoint for initial backfill, for example `/diagnostics/console-logs/recent`.
- **FR-017**: The backend MUST expose an authenticated console sources endpoint, for example `/diagnostics/console-logs/sources`.
- **FR-018**: The backend MUST expose an authenticated live console logs SignalR hub, for example `/elsa/hubs/diagnostics/console-logs`.
- **FR-019**: Recent queries and live subscriptions MUST support filters for source ID, stream, free-text query, time range, and maximum result count.
- **FR-020**: The live hub MUST allow a caller to subscribe, update filters without reconnecting, and unsubscribe.
- **FR-021**: The live hub MUST send console line events, dropped-line summaries, and source status changes to subscribed callers.

**Authorization and redaction**

- **FR-022**: Recent-line endpoints, source-listing endpoints, and live console log hubs MUST all require the same dedicated permission such as `read:diagnostics:console-logs`.
- **FR-023**: The feature MUST use Elsa's existing authentication, authorization, and cross-origin access patterns.
- **FR-024**: Redaction MUST run before console lines or source metadata are stored in recent buffers, streamed live, or returned by endpoints.
- **FR-025**: Default redaction MUST mask common secret indicators such as authorization, bearer tokens, passwords, secrets, API keys, cookies, connection strings, and similarly named values.
- **FR-026**: Hosts MUST be able to configure additional redaction rules and replacement text.
- **FR-027**: Redaction MUST apply to raw line text and to sensitive source metadata fields before data leaves the backend.
- **FR-027a**: Console log providers MUST receive, store, stream, and list only redacted console line text and redacted source metadata; raw unredacted console content MUST remain inside the capture/redaction boundary.

**Source identity and provider boundaries**

- **FR-028**: Every console line MUST include a source descriptor with source ID, display name, service name, process ID, machine name, and, when available, pod name, container name, namespace, and node name.
- **FR-029**: Sources MUST expose last-seen time and health such as connected, stale, or disconnected.
- **FR-030**: The default provider MUST support in-process capture for local development, tests, and single-node hosts.
- **FR-031**: The provider boundary MUST allow future shared or external providers to aggregate console lines across multiple Core instances without changing Studio-facing contracts.
- **FR-032**: Direct Kubernetes, Docker, orchestrator log API, vendor sink, and OpenTelemetry integrations MUST remain out of scope for this feature unless added by later specs.
- **FR-033**: Merged streams MUST provide deterministic ordering when source timestamps overlap or clocks differ, using received order or another stable tiebreaker.

**Configuration and operations**

- **FR-034**: The feature MUST provide host options for recent buffer capacity, subscriber/channel capacity, maximum recent query size, maximum line length, idle flush timeout, ANSI handling, source heartbeat timeout, redaction rules, and provider selection.
- **FR-035**: Provider failures MUST be surfaced to recent queries or live subscriptions with safe error information and without exposing unredacted console content.
- **FR-036**: The feature MUST include validation coverage for capture, stream identity, filtering, buffering, dropped counts, authorization, redaction, source health, provider boundaries, and feedback-loop prevention.

### Key Entities *(include if feature involves data)*

- **Console Log Line**: A redacted raw stdout or stderr line with sequence, timestamps, stream identity, text, truncation state, source identity, and dropped-line context.
- **Console Log Source**: The process, pod, container, machine, or external provider source that produced console output.
- **Console Log Filter**: Criteria used for recent queries and live subscriptions, including source, stream, text, time range, and take limit.
- **Console Log Provider**: Replaceable provider that stores recent console lines, streams future lines, lists sources, and reports dropped-line/source status.
- **Console Capture Tee**: Capture boundary that observes stdout/stderr while preserving the original console destination.
- **Console Redaction Rule**: Configured matching rule that masks sensitive raw line text or source metadata before exposure.
- **Console Log Subscription**: Live connection with active filters, bounded queue behavior, and dropped-line summaries.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In single-process validation, authorized subscribers receive 100% of complete stdout and stderr test lines up to configured subscriber capacity within 1 second of capture.
- **SC-002**: Recent-line queries never return more than the configured maximum number of lines.
- **SC-003**: Unauthorized callers are rejected for 100% of recent-line, source-list, and live hub access attempts.
- **SC-004**: Default redaction masks common secret patterns in raw console line text and sensitive source metadata in validation tests.
- **SC-005**: Under sustained overload, memory remains bounded and dropped-line counts are visible through recent results or live summaries.
- **SC-006**: In simulated multi-source validation, merged results include lines from all active sources and source filtering returns only the selected source.
- **SC-007**: A source that stops sending lines or heartbeats is marked stale within the configured heartbeat timeout.
- **SC-008**: Console writes continue reaching the host's original stdout/stderr destination while capture is enabled.
- **SC-009**: Documentation or feature metadata clearly separates console logs from structured logs, structured log persistence, and future OpenTelemetry diagnostics.

## Assumptions

- Studio will implement a paired feature spec in the `elsa-studio` repository and is owned by another worker.
- The Core route names, hub route, remote feature name, and permission use diagnostics console logs naming unless later renamed consistently before implementation.
- In-process capture is the first slice; cluster-ready behavior is provided through source identity and provider boundaries rather than direct orchestrator APIs.
- Recent console history is operational troubleshooting data, not durable audit logging.
- Redacted console lines are the only form exposed by providers, endpoints, hubs, and buffers.
- Hosts that need long-term log retention should continue using their existing observability or platform log systems.
