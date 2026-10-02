# Feature Specification: Diagnostics Console Logs Studio Module

**Feature Branch**: `006-diagnostics-console-logs`  
**Created**: 2026-05-18  
**Status**: Draft  
**Input**: User description: "Create a Studio feature spec for diagnostics console streaming based on the existing roadmap at specs/diagnostics-console-streaming-roadmap.md and the surrounding context from specs/003-live-server-logs and specs/004-diagnostics-structured-logs. Studio owns Elsa.Studio.Diagnostics.ConsoleLogs with route /diagnostics/console, Diagnostics navigation label Console, feature gating, API client, SignalR client, console viewer UX, source/stream/text filters, states, and URL state. Do not touch elsa-core or implementation code."

## Clarifications

### Session 2026-05-18

- Q: Which query parameters should Studio use as the canonical URL state for Console filters and viewer controls? → A: Use `source`, `stream`, `text`, `from`, `to`, `wrap`, `compact`, `ansi`, and `follow`.
- Q: How should Studio handle Diagnostics navigation when the backend feature or permission is unavailable? → A: Hide nav, route state.
- Q: How should Studio apply the `text` filter to recent and live console lines? → A: Backend-filter recent and live streams by `text`; Studio highlights returned visible matches.
- Q: What format should Console URL time filters use for `from` and `to`? → A: ISO-8601 UTC timestamps.
- Q: What source identity should Studio use for source filters, live subscriptions, and stale-source selection? → A: Stable backend `source.id`; labels use `displayName` and metadata.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Watch live console output (Priority: P1)

A developer or administrator opens Studio's Diagnostics Console page and sees recent backend stdout and stderr lines followed by live updates in a dense, terminal-like viewer.

**Why this priority**: The primary value of console streaming is immediate visibility into raw backend process output during local development and operational investigation.

**Independent Test**: Run Studio against a backend that advertises diagnostics console logs, provide recent and live console lines, and verify the page loads backfill, connects to live streaming, and renders stdout and stderr distinctly.

**Acceptance Scenarios**:

1. **Given** the backend advertises diagnostics console logs, **When** the user opens `/diagnostics/console`, **Then** Studio displays recent console lines and starts live streaming without requiring a manual refresh.
2. **Given** live tailing is enabled and the user is at the bottom of the viewer, **When** new console lines arrive, **Then** the viewer appends them and follows the tail.
3. **Given** live output includes stdout and stderr, **When** lines render in the viewer, **Then** each line clearly identifies its stream and stderr is visually distinguishable from stdout.

---

### User Story 2 - Control and preserve the viewing session (Priority: P2)

An operator pauses noisy output, resumes tailing, clears only the local view, reconnects after a lost connection, copies visible lines, exports visible lines, and toggles wrapping or compact density without losing the selected filters.

**Why this priority**: Console output can be high volume and transient, so users need reliable controls to inspect a moment in time without changing backend capture behavior.

**Independent Test**: Use mocked recent and live console lines, then exercise pause/resume, follow-tail, clear local view, reconnect, copy, export, wrap, compact mode, and local row cap behavior.

**Acceptance Scenarios**:

1. **Given** the stream is paused, **When** new lines arrive, **Then** Studio does not move the visible viewport and indicates that newer lines are waiting.
2. **Given** the user clears the local view, **When** the backend continues producing output, **Then** only the client-side rendered lines are removed and new live lines continue to appear.
3. **Given** the local visible-row cap is exceeded, **When** older rows are discarded from the page, **Then** Studio shows that local rows were discarded while preserving the active stream.

---

### User Story 3 - Filter by source, stream, and text (Priority: P3)

An operator starts in an `All sources` merged view, narrows to one source when investigating a specific process or pod, filters stdout or stderr, searches text with highlighted matches, and shares or bookmarks the filtered view.

**Why this priority**: Clustered and noisy deployments require source-aware filtering and shareable state to make raw console streams usable.

**Independent Test**: Mock multiple console sources with different health states, streams, and line text; verify source, stream, text, and time filters affect recent loading, live streaming, rendered rows, and URL query state.

**Acceptance Scenarios**:

1. **Given** multiple sources are available, **When** the Console page loads without a source filter, **Then** Studio defaults to `All sources` and identifies each line's source.
2. **Given** the user selects one source, **When** recent and live lines load, **Then** only lines for that source are visible and future live updates use the same source filter.
3. **Given** a text filter is present in the URL, **When** the Console page loads, **Then** Studio applies the filter, highlights matches, and shows a no-match state if no visible lines match.

### Edge Cases

- Backend does not advertise the diagnostics console logs feature.
- User lacks permission to read diagnostics console logs.
- Recent-line loading succeeds but live streaming fails.
- Live streaming disconnects, reconnects, or loses authorization after the page has rendered.
- The selected source becomes stale or disconnected while still selected.
- Filter values include spaces, quotes, punctuation, or other characters requiring URL encoding.
- Console lines are very long, include stack traces, contain ANSI escape sequences, or were truncated by the backend.
- Console output arrives faster than the viewer can render locally.
- The user's filters match no recent or live lines.
- Old structured-log bookmarks are opened by mistake; Console must remain distinct from Structured Logs.

## Requirements *(mandatory)*

### Functional Requirements

**Module identity, routing, and feature gating**

- **FR-001**: The Studio module identity MUST be `Elsa.Studio.Diagnostics.ConsoleLogs`.
- **FR-002**: The Console page route MUST be `/diagnostics/console`.
- **FR-003**: The navigation entry MUST be labeled `Console` and SHOULD appear in the Diagnostics navigation area alongside, but separate from, Structured Logs.
- **FR-004**: The Console page MUST gate visibility and direct navigation on the paired backend diagnostics console logs remote feature; when unavailable, Studio MUST hide the Diagnostics `Console` navigation entry while direct `/diagnostics/console` visits show the appropriate unavailable or unauthorized page state.
- **FR-005**: The Console page MUST require the diagnostics console logs read permission exposed by the paired backend capability.
- **FR-006**: The Console module MUST remain separate from `Elsa.Studio.Diagnostics.StructuredLogs` and MUST NOT present stdout or stderr lines as structured log records.

**Data access and streaming**

- **FR-007**: Studio MUST load recent console lines before starting live streaming so users see context immediately.
- **FR-008**: Studio MUST retrieve the known console sources and keep source metadata available for filtering and row display.
- **FR-009**: Studio MUST provide a diagnostics console logs API client contract for recent lines, source lists, and filterable backfill.
- **FR-010**: Studio MUST provide a diagnostics console logs SignalR client contract for the paired backend's authenticated live console stream using the repository's existing authenticated streaming patterns and server-side `text` filtering.
- **FR-011**: Studio MUST update the live subscription when source, stream, text, or time filters change.
- **FR-012**: Studio MUST disconnect from live streaming and reload remote capability state when the page is disposed, the selected backend changes, or the user explicitly leaves the active stream.
- **FR-013**: Studio MUST represent received console lines with line ID, timestamp, received timestamp when available, sequence, stream, text, source, truncation status, and dropped-before count when provided by the backend.

**Console viewer UX**

- **FR-014**: The viewer MUST show console lines in a dense, scan-friendly layout with timestamp, stream, source identity, and raw text.
- **FR-015**: The viewer MUST distinguish stdout from stderr through visible labels and styling.
- **FR-016**: The viewer MUST provide pause/resume, follow-tail, clear local view, reconnect, copy visible lines, export visible lines, wrap toggle, compact toggle, and raw ANSI sequence toggle controls.
- **FR-017**: The viewer MUST preserve raw console text and MUST NOT parse lines into semantic log fields such as level, category, template, properties, scopes, trace, or span.
- **FR-018**: Text search MUST be sent to both recent backfill and live stream subscriptions as the server-side `text` filter, and Studio MUST highlight matching text in returned visible lines without modifying the underlying raw line text.
- **FR-019**: Very long lines, multiline-looking content, stack traces, ANSI sequences, and truncated lines MUST remain readable without breaking the page layout.
- **FR-020**: The viewer MUST cap locally rendered rows and visibly indicate when older local rows or backend-dropped lines are no longer shown.

**Filtering, sources, and URL state**

- **FR-021**: The source selector MUST include `All sources` plus every backend-reported source and MUST use the stable backend `source.id` for source filters, live subscriptions, URL state, and stale-source selection.
- **FR-022**: Sources MUST display useful identity using `displayName` plus available service, process, machine, pod, container, namespace, or node metadata.
- **FR-023**: Stale or disconnected sources MUST remain visible and selectable with a clear health indicator.
- **FR-024**: Filters MUST support source, stream (`stdout`, `stderr`, or both), text search, and time range, with URL `from` and `to` values represented as ISO-8601 UTC timestamps.
- **FR-025**: Primary filter and viewer settings MUST be preserved in the URL query string so the view can be refreshed, bookmarked, and shared, using canonical parameters `source`, `stream`, `text`, `from`, `to`, `wrap`, `compact`, `ansi`, and `follow`.
- **FR-026**: URL state MUST be validated before use so invalid source, stream, time, or setting values fall back to safe defaults with visible feedback when helpful.

**States and feedback**

- **FR-027**: The page MUST show distinct states for feature unavailable, unauthorized, loading recent lines, connecting, connected, paused, reconnecting, disconnected, generic API or stream error, empty, no matches, stale source, and partial data due to dropped or truncated lines.
- **FR-028**: Direct navigation to `/diagnostics/console` without the required backend feature or permission MUST show a clear unavailable or unauthorized state without unhandled errors.
- **FR-029**: Reconnect actions MUST preserve active filters and URL state.
- **FR-030**: Copy and export actions MUST operate on the current visible rows and include enough timestamp, stream, and source context for the copied or exported text to be useful outside Studio.

### Key Entities

- **Console Log Line**: A raw stdout or stderr line shown in Studio with timing, stream, source, sequence, truncation, and dropped-line context.
- **Console Log Source**: A backend process, service, pod, or container source reported by the paired backend capability, including stable `source.id`, `displayName`, identity metadata, and health metadata.
- **Console Log Filter State**: Source, stream, text, time range, and viewer settings stored in component state and URL query parameters.
- **Console Log Stream Session**: The live client connection, active filters, connection state, and local buffering state for one Console page session.
- **Console Viewer Buffer**: The local visible row collection, discarded-row count, pause state, pending-line indicator, and export/copy selection basis.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Opening `/diagnostics/console` against an enabled local backend displays recent lines and begins live updates within 2 seconds.
- **SC-002**: A mocked three-source backend defaults to `All sources` and correctly filters recent and live lines to each individual source.
- **SC-003**: Stream filtering can show stdout only, stderr only, or both, and the visible rows match the selected streams in 100% of mocked filter cases.
- **SC-004**: Text filtering highlights matches and shows a no-match state when zero visible lines match, without a full page reload.
- **SC-005**: Refreshing or sharing a URL with source, stream, text, time, wrap, compact, raw ANSI, and follow-tail state restores those settings.
- **SC-006**: The viewer remains responsive with 10,000 local console lines by enforcing the configured visible-row cap and reporting discarded local rows.
- **SC-007**: Direct navigation without the backend feature or required permission shows the correct unavailable or unauthorized state without unhandled exceptions.
- **SC-008**: Copy and export actions include timestamp, stream, source, and text for every visible row included in the action.

## Assumptions

- The paired backend feature is owned outside this Studio spec and will provide the diagnostics console logs remote feature, permission, recent-line contract, source-list contract, and live stream contract.
- The backend feature name and permission are diagnostics-specific and distinct from Structured Logs; the expected permission is `read:diagnostics:console-logs`.
- Studio exports only the current local visible buffer in the first slice; backend-generated download endpoints are outside this Studio spec.
- ANSI escape sequences are preserved in raw line text, and Studio offers a display toggle so users can choose raw ANSI sequences or stripped plain text display.
- `All sources` is the default source filter, and both stdout and stderr are shown by default.
- The Console module does not include direct Kubernetes, Docker, orchestrator log API integration, trace waterfalls, metrics, or OpenTelemetry exploration.
