# Feature Specification: Diagnostics Structured Logs

**Feature Branch**: `004-diagnostics-structured-logs`  
**Created**: 2026-05-10  
**Status**: Draft  
**Input**: User description: "Refactor the current server log streaming module into Elsa.Diagnostics.StructuredLogs, keep it as the structured logging module, and prepare separate future specs for console streaming and OpenTelemetry exploration."

## Clarifications

### Session 2026-05-10

- Q: Should the current module become raw console streaming? -> A: No. Keep it as structured logging and specify raw stdout/stderr console streaming separately later.
- Q: What should the diagnostic namespace umbrella be? -> A: Use `Elsa.Diagnostics.*`.
- Q: What should this module be called? -> A: `Elsa.Diagnostics.StructuredLogs`.
- Q: Should OpenTelemetry visualization be part of this module? -> A: No. This module should expose trace/span correlation fields and links, while a future `Elsa.Diagnostics.OpenTelemetry` module owns trace and metric exploration.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Install a clearly named structured logs module (Priority: P1)

An Elsa host developer enables structured log streaming through a diagnostics-specific module name and API that no longer suggests raw server console capture.

**Why this priority**: The current name conflates structured `ILogger` events with stdout/stderr console logs. A precise module identity prevents the wrong user expectations before adding console streaming as a separate module.

**Independent Test**: Build a sample host using the renamed package, namespace, feature class, fluent extension, shell feature, endpoint mappings, and installed feature name without references to the previous `Elsa.ServerLogs` module identity.

**Acceptance Scenarios**:

1. **Given** an Elsa host references the diagnostics structured logs package, **When** the host enables the old-style module API, **Then** it uses diagnostics-specific names such as `UseStructuredLogs` from `Elsa.Diagnostics.StructuredLogs`.
2. **Given** a shell-based host configures features through appsettings, **When** it enables the structured logs shell feature, **Then** the feature appears under a diagnostics structured logs remote feature name.
3. **Given** Studio checks installed backend features, **When** structured logs are enabled, **Then** the advertised remote feature name uniquely identifies structured logs and does not mention console streaming.

---

### User Story 2 - Preserve semantic log data for inspection (Priority: P2)

An operator inspects logs as structured records with levels, categories, message templates, rendered messages, scopes, properties, exceptions, and workflow/correlation context.

**Why this priority**: The module's value is semantic logging. If it only renders formatted text, it overlaps with the future console logs module and loses the Aspire-style structured log experience.

**Independent Test**: Emit `ILogger` records with message templates, named properties, scopes, exceptions, workflow context, and active `Activity` trace/span IDs; verify the captured event contains the semantic fields after redaction.

**Acceptance Scenarios**:

1. **Given** an `ILogger` call uses a message template and named arguments, **When** the event is captured, **Then** the module stores both the rendered message and the original message template plus named properties.
2. **Given** an `ILogger` scope is active, **When** a log event is captured inside the scope, **Then** the event includes scope values unless redaction removes them.
3. **Given** a log event occurs inside an active trace/span, **When** the event is captured, **Then** trace ID and span ID are exposed for Studio and future OpenTelemetry cross-links.

---

### User Story 3 - Keep the structured logs contract separate from console logs and telemetry exploration (Priority: P3)

An operator understands which diagnostics surface they are using and can rely on stable boundaries between structured logs, raw console logs, and future OpenTelemetry views.

**Why this priority**: The diagnostics area is expected to grow. Strong boundaries now avoid naming churn and duplicated responsibilities later.

**Independent Test**: Review API routes, hub routes, models, permissions, documentation, and feature metadata to confirm this module only promises structured logs and explicitly excludes stdout/stderr capture and trace visualization.

**Acceptance Scenarios**:

1. **Given** the structured logs module is enabled, **When** an activity writes directly to stdout without `ILogger`, **Then** this module is not expected to capture it.
2. **Given** a user needs raw stdout/stderr, **When** they read this module's README or quickstart, **Then** it points to the future Console Logs/Console Streaming module rather than claiming console capture.
3. **Given** a user needs trace waterfall or metrics charts, **When** they read this module's README or quickstart, **Then** it identifies those capabilities as future OpenTelemetry module responsibilities.

### Edge Cases

- Existing unpublished code, docs, and specs still use `Elsa.ServerLogs` or `ServerLogStreaming`.
- Shell feature names are persisted in appsettings and must be intentionally changed before release.
- API clients generated from the old route names may still exist in Studio.
- Scope values can be nested objects, anonymous objects, dictionaries, or non-string values.
- Message templates can be missing when log events originate from custom providers.
- Redaction can remove fields that would otherwise be used for filtering or trace links.
- Structured log capture can recursively capture its own internal diagnostics logs.

## Requirements *(mandatory)*

### Functional Requirements

**Naming and module identity**

- **FR-001**: The Core module project MUST be renamed from `Elsa.ServerLogs` to `Elsa.Diagnostics.StructuredLogs`.
- **FR-002**: The root namespace MUST be `Elsa.Diagnostics.StructuredLogs`.
- **FR-003**: Public types that currently use `ServerLog` or `ServerLogs` MUST be renamed to `StructuredLog` or `StructuredLogs` unless the old name is retained only as an explicitly obsolete compatibility shim.
- **FR-004**: Public types that currently use `ServerLogStreaming` MUST be renamed to `StructuredLogs` or `StructuredLogStreaming` only where "streaming" describes a transport detail.
- **FR-005**: The old-style feature class MUST be renamed to a structured logs feature, with a fluent extension such as `UseStructuredLogs`.
- **FR-006**: The shell feature MUST be renamed to a structured logs shell feature and MUST expose configuration through bindable public properties only.
- **FR-007**: The remote installed-feature name MUST become diagnostics-specific, for example `Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogsFeature`, and Studio MUST be able to gate against that value.
- **FR-008**: Package metadata, README files, sample host wiring, solution entries, project references, test project names, and docs MUST use the diagnostics structured logs name.

**Structured log capture**

- **FR-009**: The module MUST continue to capture structured `ILogger` events through an `ILoggerProvider`.
- **FR-010**: Captured events MUST include timestamp, received timestamp, sequence, level, category, event ID, event name, rendered message, message template, exception summary/detail, scopes, structured properties, trace ID, span ID, correlation ID, tenant ID, workflow definition ID, workflow instance ID, and source ID when available.
- **FR-011**: The logger provider MUST populate message template values from `{OriginalFormat}` when present.
- **FR-012**: The logger provider MUST capture active logging scopes, including dictionary-like and key-value scope values.
- **FR-013**: The module MUST preserve named structured properties separately from the rendered message.
- **FR-014**: The module MUST keep a bounded recent-history buffer and bounded subscriber behavior with visible dropped-event counts.
- **FR-015**: The module MUST prevent recursive capture of its own diagnostics logs by default.

**API, hub, and permissions**

- **FR-016**: REST and SignalR route names SHOULD move from server-log naming to diagnostics structured-log naming, for example `/elsa/api/diagnostics/structured-logs` and `/elsa/hubs/diagnostics/structured-logs`.
- **FR-017**: The permission name MUST move from `read:server-logs` to a diagnostics structured logs permission such as `read:diagnostics:structured-logs`.
- **FR-018**: The API contract MUST continue to support recent queries, source listing, live subscriptions, filter updates, and dropped-event summaries.
- **FR-019**: Filters MUST continue to support minimum level, exact levels, category prefix, free-text query, tenant ID, workflow definition ID, workflow instance ID, trace ID, span ID, correlation ID, source ID, and time range.
- **FR-020**: Trace ID and span ID fields MUST be stable enough for future `Elsa.Diagnostics.OpenTelemetry` deep links.

**Configuration and safety**

- **FR-021**: Configuration options MUST be renamed from server-log wording to structured-log wording.
- **FR-022**: Shell feature properties MUST mirror bindable structured log options and copy those values into the service options during service registration.
- **FR-023**: Redaction MUST continue to run before buffering or streaming.
- **FR-024**: Default redaction MUST continue to mask common secret names and values in messages, exceptions, scopes, and structured properties.
- **FR-025**: The module MUST explicitly document that direct stdout/stderr capture is out of scope and belongs to a future diagnostics console logs module.

### Key Entities *(include if feature involves data)*

- **Structured Log Event**: A semantic log record captured from `ILogger`.
- **Structured Log Source**: The process, pod, container, machine, or backend source that produced a structured log event.
- **Structured Log Filter**: Query/subscription criteria for recent and live structured logs.
- **Structured Log Provider**: Pluggable source of recent and live structured log events.
- **Structured Log Redactor**: Redaction service applied before events leave the backend.
- **Structured Log Subscription**: Live SignalR subscription plus mutable filters and backpressure state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: No production source file, test file, project file, or README in the structured logs module retains `Elsa.ServerLogs` as the active namespace, assembly, package, or module name.
- **SC-002**: A host can enable the renamed module with old-style code configuration and shell-based appsettings configuration.
- **SC-003**: Studio can detect the renamed remote feature and load structured logs without relying on the old remote feature name.
- **SC-004**: Tests verify message template capture from `{OriginalFormat}`.
- **SC-005**: Tests verify logging scope capture and redaction.
- **SC-006**: Existing recent query, source list, filtering, live stream, and dropped-event behavior continue to pass after the rename.
- **SC-007**: Documentation clearly separates structured logs from future console logs and OpenTelemetry explorer modules.

## Assumptions

- The current branch has not shipped as a stable public package, so breaking renames are acceptable if performed consistently.
- Studio will implement a paired `Elsa.Studio.Diagnostics.StructuredLogs` spec under the same feature ID.
- Console streaming will be specified separately as `Elsa.Diagnostics.ConsoleLogs` or an equivalent final name.
- OpenTelemetry visualization will be specified separately as `Elsa.Diagnostics.OpenTelemetry`.
- Long-term durable log storage remains out of scope for this module unless provided later by a pluggable provider.
