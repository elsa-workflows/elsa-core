# Feature Specification: Diagnostics Structured Logs Studio Module

**Feature Branch**: `004-diagnostics-structured-logs`  
**Created**: 2026-05-10  
**Status**: Clarified  
**Input**: User description: "Refactor the current Server Logs Studio module into a diagnostics structured logs module, keep it separate from future console streaming and OpenTelemetry explorer modules, and improve the structured logging direction."

## Clarifications

### Session 2026-05-10

- Q: Should the current Studio module become the raw console viewer? -> A: No. It remains the structured log viewer.
- Q: What should the Studio module be called? -> A: Use Studio conventions with `Elsa.Studio.Diagnostics.StructuredLogs`, paired with Core's `Elsa.Diagnostics.StructuredLogs`.
- Q: Should the UI look like a terminal? -> A: Only where dense scanning helps. The product concept is now an Aspire-style structured/semantic logs viewer, not stdout/stderr console tailing.
- Q: Should trace waterfalls and metric charts be part of this module? -> A: No. This module may link by trace/span ID, while a future `Elsa.Diagnostics.OpenTelemetry`/Studio counterpart owns traces and metrics.
- Clarify pass result: No remaining product or engineering ambiguities block planning. Use consistent breaking renames for this unpublished feature branch, prefer `/diagnostics/structured-logs` as the canonical route, and treat the old `/server-logs` route only as a development bookmark redirect/unavailable compatibility path if the current router makes that practical.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Navigate to a clearly named Structured Logs page (Priority: P1)

An administrator or developer sees a Diagnostics area in Studio and opens Structured Logs, understanding that the page shows semantic log records from the backend rather than raw stdout/stderr console output.

**Why this priority**: The current "Server Logs" naming and console-like styling imply raw console capture. Renaming and repositioning the module establishes the right mental model before adding a separate Console Logs module.

**Independent Test**: Run Studio against a backend advertising the renamed structured logs feature and verify the menu, route, page title, feature gating, API client, SignalR client, and static assets all use diagnostics structured logs naming.

**Acceptance Scenarios**:

1. **Given** the backend advertises the renamed structured logs remote feature, **When** Studio loads navigation, **Then** a Structured Logs entry appears under Diagnostics.
2. **Given** the backend does not advertise the renamed structured logs feature, **When** Studio loads navigation, **Then** the Structured Logs entry is hidden or direct navigation shows a clear unavailable state.
3. **Given** a user opens Structured Logs, **When** the page renders, **Then** the page title, route, and empty/error states use structured logs wording and do not mention console streaming.

---

### User Story 2 - Inspect semantic log records (Priority: P2)

An operator scans structured log rows and opens details to inspect message template, rendered message, named properties, scopes, exception details, source metadata, and trace/span IDs.

**Why this priority**: The structured logs module should provide the semantic detail that raw console streaming cannot.

**Independent Test**: Mock recent/live structured log events with templates, properties, scopes, exceptions, trace/span IDs, workflow IDs, and sources; verify rows and details render the fields with copy actions and without layout overflow.

**Acceptance Scenarios**:

1. **Given** a structured log event has a message template and properties, **When** the user opens the details view, **Then** Studio shows the rendered message, template, and property list distinctly.
2. **Given** a structured log event has scope values, **When** the user opens the details view, **Then** Studio shows scopes separately from properties.
3. **Given** a structured log event has an exception, **When** the user opens the details view, **Then** Studio shows exception type, message, and stack trace in a readable layout.
4. **Given** a structured log event has trace/span IDs, **When** the user views the row or details, **Then** Studio exposes copyable trace/span values and reserves a future deep-link target for the OpenTelemetry module.

---

### User Story 3 - Filter and correlate structured logs (Priority: P3)

An operator narrows structured logs by level, category, message text, source, workflow context, tenant, correlation ID, trace ID, span ID, and time range.

**Why this priority**: Structured logs become valuable when users can slice the semantic fields rather than scroll a raw text stream.

**Independent Test**: Use mocked REST and SignalR contracts with multiple levels, categories, sources, workflow IDs, trace IDs, and properties; verify filters update recent queries, live subscriptions, URL state, and visible rows.

**Acceptance Scenarios**:

1. **Given** logs at multiple levels and categories, **When** the user changes level or category filters, **Then** Studio reloads recent rows and updates the live subscription.
2. **Given** a trace ID filter is present in the URL, **When** the Structured Logs page loads, **Then** the page shows logs for that trace without extra user input.
3. **Given** the user opens logs from a workflow instance context, **When** the page loads, **Then** it is filtered to that workflow instance and the filter is visible.

### Edge Cases

- Backend still advertises only the old `Elsa.ServerLogs` feature during a partial rollout.
- Old bookmarked `/server-logs` URLs exist from development builds.
- Rows contain large property dictionaries or long stack traces.
- Log entries arrive faster than the UI can render.
- Filter values contain characters that need URL encoding.
- Trace/span IDs exist but the OpenTelemetry module is not installed yet.
- Source names are long Kubernetes pod names.
- The user's filters match no events.

## Requirements *(mandatory)*

### Functional Requirements

**Naming, routing, and feature gating**

- **FR-001**: The Studio module project MUST be renamed from `Elsa.Studio.ServerLogs` to `Elsa.Studio.Diagnostics.StructuredLogs`.
- **FR-002**: The root namespace MUST be `Elsa.Studio.Diagnostics.StructuredLogs`.
- **FR-003**: Public types that currently use `ServerLog` or `ServerLogs` MUST be renamed to `StructuredLog` or `StructuredLogs` unless kept only as explicitly obsolete compatibility shims.
- **FR-004**: The service registration extension MUST be renamed from `AddServerLogsModule` to a diagnostics structured logs name such as `AddStructuredLogsModule`.
- **FR-005**: The Studio feature MUST gate on Core's renamed diagnostics structured logs remote feature name.
- **FR-006**: The UI route SHOULD move from `/server-logs` to `/diagnostics/structured-logs`.
- **FR-007**: The navigation item MUST be labeled `Structured Logs` and SHOULD appear in a Diagnostics navigation group if the current navigation model supports grouping.
- **FR-008**: Host project references, bundle references, imports, README files, static asset paths, and tests MUST use the new Studio diagnostics structured logs name.

**Data access**

- **FR-009**: Studio MUST call the renamed diagnostics structured logs recent endpoint before starting the live subscription.
- **FR-010**: Studio MUST connect to the renamed diagnostics structured logs SignalR hub using existing authenticated SignalR configuration patterns.
- **FR-011**: Studio MUST update the active hub subscription when filters change.
- **FR-012**: Studio MUST disconnect from the hub when the component is disposed or the selected backend changes.
- **FR-013**: Studio models MUST include event ID, event name, rendered message, message template, exception, scopes, properties, trace ID, span ID, correlation ID, tenant ID, workflow definition ID, workflow instance ID, and source ID when the backend sends them.

**Structured logs UX**

- **FR-014**: The page MUST present structured log rows with columns for timestamp, level, category, source, trace/correlation hint, and rendered message.
- **FR-015**: The page MUST provide a row details panel or drawer for full semantic data: message template, rendered message, properties, scopes, exception detail, source metadata, trace/span IDs, workflow IDs, tenant ID, and raw JSON/copy output.
- **FR-016**: The page MUST preserve dense scanning controls from the current viewer where still useful: pause/resume, clear local view, reconnect, copy selected or visible rows, auto-scroll, wrap, and compact mode.
- **FR-017**: The page MUST avoid presenting itself as raw stdout/stderr console output.
- **FR-018**: Long messages, property values, and stack traces MUST not break the page layout.
- **FR-019**: Empty, disconnected, reconnecting, unauthorized, feature-unavailable, and no-match states MUST be visible and distinct.

**Filtering and correlation**

- **FR-020**: Filters MUST support minimum level, exact levels, category prefix, free-text query, tenant ID, workflow definition ID, workflow instance ID, trace ID, span ID, correlation ID, source ID, and time range.
- **FR-021**: Primary filters MUST be preserved in the URL query string.
- **FR-022**: Studio SHOULD provide contextual links from workflow instance views to Structured Logs filtered to that workflow instance.
- **FR-023**: Trace and span IDs MUST be copyable and SHOULD be represented as future deep links to the OpenTelemetry Explorer when that module is installed.
- **FR-024**: Source filtering MUST continue to support merged view and individual source selection.
- **FR-025**: Stale or disconnected sources MUST remain visible with a health indicator.

### Key Entities

- **Structured Log Event**: UI representation of the backend structured log event.
- **Structured Log Source**: Backend source reported by the diagnostics structured logs backend.
- **Structured Log Filter State**: Filter values stored in component state and URL query string.
- **Structured Log Subscription**: Client-side SignalR connection plus active filters and connection status.
- **Structured Log Details**: Expanded semantic view of properties, scopes, exception, source, and correlation fields.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Opening Structured Logs against an enabled backend displays recent logs and connects live within 2 seconds on a local development backend.
- **SC-002**: No production source file, project file, host reference, README, route label, or static asset path in the renamed Studio module uses `Elsa.Studio.ServerLogs` as the active module identity.
- **SC-003**: Studio hides or marks the page unavailable when the renamed Core remote feature is missing.
- **SC-004**: Structured log details show message template, properties, scopes, exception detail, trace ID, and span ID from mocked events.
- **SC-005**: Filter changes update both recent queries and live subscriptions without a full page reload.
- **SC-006**: A mocked three-source backend defaults to merged view and correctly filters to each individual source.
- **SC-007**: Documentation clearly separates Structured Logs from future Console Logs and OpenTelemetry Explorer modules.

## Assumptions

- Core will provide the paired `Elsa.Diagnostics.StructuredLogs` contract under this same feature ID.
- The current development branch has not shipped as a stable public Studio module, so breaking renames are acceptable if performed consistently.
- The future console viewer will use a separate Studio module, likely `Elsa.Studio.Diagnostics.ConsoleLogs`.
- The future OpenTelemetry viewer will use a separate Studio module, likely `Elsa.Studio.Diagnostics.OpenTelemetry`.
- The first structured logs UI may remain a Blazor list/table with a details panel; virtualization can be added if the visible row cap is insufficient.
