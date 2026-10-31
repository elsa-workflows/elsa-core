# Feature Specification: Server Logs Studio Module

**Feature Branch**: `003-live-server-logs`  
**Created**: 2026-05-06  
**Status**: Draft  
**Input**: User description: "Add an Elsa Studio module that lets users see backend console/log output like Aspire's dashboard, including merged clustered logs and individual pod views."

## Clarifications

### Session 2026-05-06

- Q: Should the UI be a raw terminal emulator? -> A: No. It should be a dense log viewer with terminal-like scanning, structured filters, and source metadata.
- Q: Which backend transport should Studio prefer? -> A: Use the paired Core feature's REST backfill and SignalR live stream.
- Q: How should clusters appear? -> A: Default to a merged stream, with an obvious source selector for individual pod/process views.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Watch live backend logs (Priority: P1)

A developer or administrator opens Studio and sees recent backend logs followed by live updates.

**Why this priority**: This is the primary user value and validates the backend integration.

**Independent Test**: Run Studio against a backend with log streaming enabled, emit logs, and verify the page renders recent and live events with auto-scroll and pause/resume controls.

**Acceptance Scenarios**:

1. **Given** the backend advertises the log-streaming feature, **When** the user opens the Server Logs page, **Then** Studio loads recent logs and starts a live subscription.
2. **Given** live tailing is enabled, **When** new events arrive, **Then** the list updates without a manual refresh and auto-scrolls when the user is already at the bottom.
3. **Given** the user pauses the stream, **When** new events arrive, **Then** Studio buffers or indicates pending events without moving the visible list until the user resumes.

---

### User Story 2 - Find relevant operational events (Priority: P2)

An operator narrows noisy logs by level, text, category, workflow instance, tenant, correlation, or source.

**Why this priority**: Real backend logs are noisy; filters make the feature usable for support and production diagnostics.

**Independent Test**: Use mocked recent/live events with multiple levels, categories, workflow IDs, tenants, and sources; verify filters update the REST query and SignalR subscription and produce expected rows.

**Acceptance Scenarios**:

1. **Given** logs at multiple levels, **When** the user selects warning and error levels, **Then** lower-severity rows are hidden and the live subscription is updated.
2. **Given** the user enters search text, **When** matching and non-matching events exist, **Then** only matching events are visible.
3. **Given** the user opens logs from a workflow instance context, **When** the logs view loads, **Then** it is pre-filtered to that workflow instance.

---

### User Story 3 - Diagnose clustered deployments by source (Priority: P3)

An operator views logs merged across all backend replicas and then focuses on one pod/process when investigating a single unhealthy replica.

**Why this priority**: Kubernetes and distributed Elsa deployments need source-aware diagnostics.

**Independent Test**: Mock the source-list and log APIs with multiple sources; verify `All sources` is the default and selecting one source updates both recent and live log filters.

**Acceptance Scenarios**:

1. **Given** the backend reports multiple sources, **When** the Server Logs page loads, **Then** the source selector defaults to `All sources` and each row displays its source.
2. **Given** the user selects a specific source, **When** recent and live logs are loaded, **Then** only events from that source are shown.
3. **Given** a source is stale or disconnected, **When** the user opens the source selector, **Then** the source remains selectable and shows its health state.

### Edge Cases

- Backend does not advertise the log-streaming feature.
- SignalR connection fails or loses authorization.
- Recent-log endpoint succeeds but live subscription fails.
- User switches backend environment while subscribed.
- Very long messages or stack traces would overflow row layout.
- High-volume streams exceed the local visible-row cap.
- Source names are long Kubernetes pod names.
- The user's filters match no events.

## Requirements *(mandatory)*

### Functional Requirements

**Module and feature gating**

- **FR-001**: Studio MUST add a Server Logs module with a route and menu item.
- **FR-002**: The module MUST be gated by the backend remote feature name exposed by the paired Core spec.
- **FR-003**: If the backend feature is unavailable, Studio MUST hide the menu item or show a clear unavailable state when directly navigated.

**Data access**

- **FR-004**: Studio MUST call the recent-log REST endpoint before starting live streaming.
- **FR-005**: Studio MUST connect to the server-log SignalR hub using `IHttpConnectionOptionsConfigurator`.
- **FR-006**: Studio MUST use `IBackendApiClientProvider.Url` to build backend hub URLs consistently with existing workflow observer behavior.
- **FR-007**: Studio MUST update the active hub subscription when filters change.
- **FR-008**: Studio MUST disconnect from the hub when the component is disposed or the selected backend changes.

**Viewer UX**

- **FR-009**: The viewer MUST show timestamp, level, category, source, message, and exception indicator for each event.
- **FR-010**: The viewer MUST support pause/resume, clear local view, reconnect, copy selected or visible rows, auto-scroll toggle, and wrap/compact toggle.
- **FR-011**: The viewer MUST support level, text, category prefix, tenant, workflow instance, correlation/trace, source, and time filters.
- **FR-012**: The viewer MUST cap rendered rows locally and indicate when older local rows were discarded.
- **FR-013**: Long messages and stack traces MUST be expandable without breaking the page layout.
- **FR-014**: Empty, disconnected, reconnecting, unauthorized, and feature-unavailable states MUST be visible and distinct.

**Cluster source UX**

- **FR-015**: The source selector MUST include `All sources` plus every backend-reported source.
- **FR-016**: Log rows MUST show source identity when more than one source exists or when source filtering is active.
- **FR-017**: Stale or disconnected sources MUST remain visible with a health indicator.

**Workflow context**

- **FR-018**: Studio SHOULD provide a link or action from workflow instance views to open Server Logs filtered to that instance.
- **FR-019**: Studio SHOULD preserve filters in the URL query string so logs can be bookmarked or shared.

### Key Entities

- **Server Log Event**: UI representation of the backend `ServerLogEvent`.
- **Server Log Source**: Pod/process/service source reported by the backend.
- **Log Filter State**: The filter values encoded in component state and URL query string.
- **Log Subscription**: Client-side SignalR connection plus active filters and connection status.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Opening Server Logs against an enabled backend displays recent logs and connects live within 2 seconds on a local development backend.
- **SC-002**: Filter changes update both visible rows and the live subscription without a full page reload.
- **SC-003**: A mocked three-source backend defaults to merged view and correctly filters to each individual source.
- **SC-004**: The component remains responsive with 10,000 local events by rendering only the configured visible cap.
- **SC-005**: Direct navigation to the route against a backend without the feature shows a clear unavailable state without unhandled exceptions.
- **SC-006**: SignalR connections are disposed when leaving the page or switching backend environments.

## Assumptions

- The paired Core feature exposes REST and SignalR contracts under `/server-logs` and `/hubs/server-logs`.
- Studio continues to use MudBlazor/Radzen conventions already present in the repository.
- The first implementation can use a Blazor component list/table; virtualization can be added if local rendering requires it.
- The module is operational/admin functionality and should not be shown to users who lack backend permission.
