# Data Model: Diagnostics Console Logs Studio Module

## ConsoleLogLine

Represents one raw stdout or stderr line received from recent backfill or SignalR.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `string` | Stable line identity for rendering and copy/export operations. |
| `Timestamp` | `DateTimeOffset` | Backend line timestamp. |
| `ReceivedAt` | `DateTimeOffset?` | Backend receive timestamp when available. |
| `Sequence` | `long?` | Source-local or stream sequence used for ordering when provided. |
| `Stream` | `ConsoleLogStream` | `stdout` or `stderr`. |
| `Text` | `string` | Raw console text; never parsed into structured log fields. |
| `Source` | `ConsoleLogSource` | Source identity and display metadata. |
| `IsTruncated` | `bool` | Indicates backend truncated the line. |
| `DroppedBeforeCount` | `long?` | Backend-reported count of lines dropped before this line, when provided. |

### Validation Rules

- `Text` is preserved exactly for copy/export except for display-only raw ANSI stripping choices.
- `Stream` must be either `stdout` or `stderr`; invalid stream values fall back to safe defaults in URL/filter mapping.
- Long lines and multiline-looking text remain a single rendered console row unless backend line splitting has already occurred.

## ConsoleLogSource

Represents a backend process, service, pod, or container source.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `string` | Stable backend `source.id`; used for source filters, live subscriptions, URL state, and stale-source selection. |
| `DisplayName` | `string?` | User-facing label. |
| `ServiceName` | `string?` | Optional service identity. |
| `ProcessId` | `int?` | Optional process ID. |
| `MachineName` | `string?` | Optional host/machine identity. |
| `PodName` | `string?` | Optional orchestrator pod name. |
| `ContainerName` | `string?` | Optional container name. |
| `Namespace` | `string?` | Optional orchestrator namespace. |
| `NodeName` | `string?` | Optional node identity. |
| `LastSeenAt` | `DateTimeOffset?` | Optional source heartbeat/recency timestamp. |
| `Health` | `ConsoleLogSourceHealth` | `Connected`, `Stale`, `Disconnected`, or `Unknown`. |

### Validation Rules

- A source without a matching current backend source list may remain selectable when it came from URL state or existing rows, but it must be shown as stale/disconnected.
- Display labels use `DisplayName` first, then available metadata, while preserving `Id` as the filter value.

## ConsoleLogFilter

Filter state shared by recent REST requests, live SignalR subscriptions, and URL query state.

| Field | Type | Notes |
|-------|------|-------|
| `SourceId` | `string?` | `null` means `All sources`; otherwise stable backend `source.id`. |
| `Streams` | `ISet<ConsoleLogStream>` | Defaults to both `stdout` and `stderr`. |
| `Text` | `string?` | Server-side text filter for recent and live data. |
| `From` | `DateTimeOffset?` | Inclusive UTC start time from URL `from`. |
| `To` | `DateTimeOffset?` | Inclusive UTC end time from URL `to`. |
| `Take` | `int?` | Recent-line limit clamped to the configured visible-row cap. |

### Validation Rules

- `From` and `To` are parsed from ISO-8601 UTC query values.
- Invalid source, stream, or time values fall back to defaults with visible feedback when helpful.
- Filter updates trigger recent reload and live subscription update while preserving viewer settings.

## ConsoleLogViewState

Local UI and viewer state for one Console page session.

| Field | Type | Notes |
|-------|------|-------|
| `Filter` | `ConsoleLogFilter` | Current source/stream/text/time filter. |
| `VisibleRows` | `IList<ConsoleLogLine>` | Bounded local rendered row buffer. |
| `VisibleRowCap` | `int` | Maximum local rows, used to prune older rows. |
| `DiscardedLocalRows` | `long` | Count of rows removed because of the local cap. |
| `ConnectionStatus` | `ConsoleLogConnectionStatus` | Feature unavailable, unauthorized, loading, connecting, connected, paused, reconnecting, disconnected, empty, no matches, stale source, or partial data. |
| `IsPaused` | `bool` | Holds live rows from moving the viewport and shows pending-line feedback. |
| `PendingLineCount` | `long` | Number of newer lines waiting while paused. |
| `FollowTail` | `bool` | Scrolls to newest rows when the user is following the tail. |
| `Wrap` | `bool` | Controls line wrapping. |
| `Compact` | `bool` | Controls density. |
| `Ansi` | `bool` | Controls raw ANSI sequence display versus stripped plain text display. |

## ConsoleLogStreamSession

Represents the live SignalR client connection and subscription state.

| Field | Type | Notes |
|-------|------|-------|
| `Filter` | `ConsoleLogFilter` | Last subscribed live filter. |
| `Status` | `ConsoleLogConnectionStatus` | Current live connection state. |
| `StartedAt` | `DateTimeOffset?` | Optional local session start. |
| `LastLineAt` | `DateTimeOffset?` | Optional timestamp of the last received line. |
| `DroppedLineSummary` | `ConsoleLogDroppedLineSummary?` | Latest backend dropped-line notification. |

### State Transitions

```text
loading recent -> connecting -> connected
connected -> paused -> connected
connected -> reconnecting -> connected
connected/reconnecting -> disconnected
any active state -> disposed
unavailable/unauthorized -> terminal page state until backend capability or permission changes
```

## RecentConsoleLinesResult

| Field | Type | Notes |
|-------|------|-------|
| `Items` | `ICollection<ConsoleLogLine>` | Recent console lines matching the filter. |
| `DroppedLineCount` | `long?` | Optional backend dropped-line count for the returned window. |
| `Sources` | `ICollection<ConsoleLogSource>?` | Optional source metadata included with the response if the backend provides it. |

## ConsoleLogDroppedLineSummary

| Field | Type | Notes |
|-------|------|-------|
| `SourceId` | `string?` | `null` means all/unknown source. |
| `DroppedLineCount` | `long` | Number of backend-dropped lines. |
| `Reason` | `string?` | Optional backend reason such as buffer overflow or subscriber lag. |
