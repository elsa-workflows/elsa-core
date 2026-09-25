# Data Model: Diagnostics Structured Logs Studio Module

## StructuredLogEvent

Represents one semantic log record received from REST or SignalR.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `string` | Stable row identity for selection and copy operations. |
| `Timestamp` | `DateTimeOffset` | Event timestamp from backend. |
| `Level` | `StructuredLogLevel` | Trace, Debug, Information, Warning, Error, Critical. |
| `Category` | `string` | Logger/category name. |
| `Message` | `string` | Rendered message. |
| `MessageTemplate` | `string?` | Original structured logging template. |
| `EventId` | `int?` | Optional .NET logging event ID. |
| `EventName` | `string?` | Optional .NET logging event name. |
| `Exception` | `StructuredLogException?` | Exception type, message, stack trace. |
| `TraceId` | `string?` | Trace correlation ID, copyable and reserved for future OpenTelemetry links. |
| `SpanId` | `string?` | Span correlation ID, copyable and reserved for future OpenTelemetry links. |
| `CorrelationId` | `string?` | Elsa/application correlation ID. |
| `TenantId` | `string?` | Tenant context. |
| `WorkflowDefinitionId` | `string?` | Workflow definition context. |
| `WorkflowInstanceId` | `string?` | Workflow instance context. |
| `SourceId` | `string` | Backend source/pod/process identity. |
| `Properties` | `IDictionary<string, object?>` | Named structured properties. |
| `Scopes` | `ICollection<object?>` or dictionary-compatible collection | Logging scopes separate from properties. |
| `Raw` | derived JSON | Raw details are generated client-side from the received event for copy/display. |

## StructuredLogException

| Field | Type | Notes |
|-------|------|-------|
| `Type` | `string?` | Exception type/full name. |
| `Message` | `string?` | Exception message. |
| `StackTrace` | `string?` | Stack trace, displayed in a wrapped/scrollable block. |

## StructuredLogSource

Represents a backend source such as a server, worker process, container, or pod.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `string` | Stable source filter value. |
| `DisplayName` | `string?` | Friendly source label; long names are truncated in rows but full value remains in title/details. |
| `Description` | `string?` | Optional source metadata. |
| `Status` | `StructuredLogSourceStatus` | Connected, stale, disconnected, unknown. |
| `LastSeen` | `DateTimeOffset?` | Optional health/recency indicator. |

## StructuredLogFilter

Filter state shared by recent REST requests, live SignalR subscriptions, and URL query state.

| Field | Type | Notes |
|-------|------|-------|
| `MinimumLevel` | `StructuredLogLevel?` | Defaults to Information unless URL/user chooses all. |
| `Levels` | `ICollection<StructuredLogLevel>?` | Optional exact level set. |
| `CategoryPrefix` | `string?` | Logger/category prefix. |
| `Text` | `string?` | Free text query over rendered message/template/properties as backend supports. |
| `TenantId` | `string?` | Tenant filter. |
| `WorkflowDefinitionId` | `string?` | Workflow definition filter. |
| `WorkflowInstanceId` | `string?` | Workflow instance filter. |
| `TraceId` | `string?` | Trace filter. |
| `SpanId` | `string?` | Span filter. |
| `CorrelationId` | `string?` | Correlation filter. |
| `SourceId` | `string?` | `null` means merged/all sources. |
| `From` | `DateTimeOffset?` | Inclusive start time. |
| `To` | `DateTimeOffset?` | Inclusive end time. |
| `Take` | `int?` | Clamped to the UI row cap for recent requests. |

## StructuredLogViewState

Local UI state.

| Field | Type | Notes |
|-------|------|-------|
| `Filter` | `StructuredLogFilter` | Current filter. |
| `VisibleRowCap` | `int` | Bounds local rows. |
| `IsPaused` | `bool` | Pauses adding live rows. |
| `AutoScroll` | `bool` | Scrolls to latest row when live rows arrive. |
| `WrapMessages` | `bool` | Controls dense row wrapping. |
| `Compact` | `bool` | Controls compact row style. |
| `ConnectionStatus` | `StructuredLogConnectionStatus` | Disconnected, connecting, connected, reconnecting, unavailable, unauthorized. |
| `LocalDroppedRows` | `int` | Count pruned due to local cap. |
| `SelectedEventId` | `string?` | Current details-panel row. |

## RecentStructuredLogsResult

| Field | Type | Notes |
|-------|------|-------|
| `Items` | `ICollection<StructuredLogEvent>` | Recent records in backend response. |
| `DroppedEventCount` | `int` | Upstream dropped events reported by backend. |
