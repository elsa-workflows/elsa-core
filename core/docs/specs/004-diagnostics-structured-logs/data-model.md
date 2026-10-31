# Data Model: Diagnostics Structured Logs

## StructuredLogEvent

Semantic log record captured from `ILogger`.

- `Id`: unique event identifier.
- `Sequence`: monotonic sequence assigned by the provider/logger path.
- `Timestamp`: event timestamp.
- `ReceivedAt`: backend receive timestamp.
- `Level`: structured log level.
- `Category`: logger category.
- `EventId`: numeric Microsoft logging event ID.
- `EventName`: optional Microsoft logging event name.
- `Message`: rendered message.
- `MessageTemplate`: original template from `{OriginalFormat}` when available.
- `Exception`: optional redacted exception summary/detail.
- `Scopes`: redacted key/value scope values from active logging scopes.
- `Properties`: redacted structured properties excluding `{OriginalFormat}`.
- `TraceId`, `SpanId`: active `Activity` identifiers for future OpenTelemetry links.
- `CorrelationId`: correlation value when available.
- `TenantId`, `WorkflowDefinitionId`, `WorkflowInstanceId`: Elsa context values when available.
- `SourceId`: producing source identifier.

## StructuredLogSource

Backend process, container, pod, or machine that produced events.

- `Id`: stable source identifier for the running process.
- `Name`: display name.
- `MachineName`, `ProcessId`, `ProcessName`: process metadata.
- `PodName`, `Namespace`, `ContainerName`, `NodeName`: Kubernetes/container metadata when available.
- `StartedAt`, `LastSeen`: source lifecycle timestamps.
- `Status`: unknown, healthy, stale, or disconnected.

## StructuredLogFilter

Criteria applied to recent queries and live subscriptions.

- `MinimumLevel`: inclusive minimum level.
- `Levels`: exact level set.
- `CategoryPrefix`: category prefix filter.
- `Query`: free-text filter across message, template, category, exception, scopes, and properties.
- `TenantId`, `WorkflowDefinitionId`, `WorkflowInstanceId`: workflow context filters.
- `TraceId`, `SpanId`, `CorrelationId`: correlation filters.
- `SourceId`: source filter.
- `From`, `To`: timestamp range.
- `Limit`: recent query limit clamped by options.

## StructuredLogProvider

Pluggable source of redacted recent and live structured log events.

- Publishes redacted `StructuredLogEvent` values.
- Returns recent events plus dropped-buffer count.
- Streams live events and dropped-event summaries.
- Lists known `StructuredLogSource` values.

## StructuredLogRedactor

Applies redaction before events leave the backend capture path.

- Masks sensitive names in properties and scopes.
- Masks sensitive text patterns in messages and exception details.
- Runs before buffering and streaming.

## StructuredLogSubscription

Live SignalR subscription state.

- Holds connection ID, current filter, cancellation token, and backpressure state.
- Can update filters without reconnecting.
- Reports dropped events when subscriber queues overflow.
