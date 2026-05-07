# Data Model: Live Server Log Streaming

## ServerLogEvent

- `Id`: provider-scoped event identifier.
- `Sequence`: monotonic sequence when the provider can assign one.
- `Timestamp`: event timestamp in UTC.
- `ReceivedAt`: server/provider receive timestamp in UTC.
- `Level`: trace, debug, information, warning, error, critical.
- `Category`: logger category.
- `EventId`: numeric/name pair when provided by `ILogger`.
- `Message`: rendered redacted message.
- `MessageTemplate`: optional redacted template.
- `Exception`: optional redacted exception summary/detail.
- `Scopes`: redacted scope values.
- `Properties`: redacted structured log properties.
- `TraceId`, `SpanId`, `CorrelationId`: diagnostic correlation.
- `TenantId`, `WorkflowDefinitionId`, `WorkflowInstanceId`: Elsa context when present.
- `SourceId`: foreign key to `ServerLogSource`.

## ServerLogSource

- `Id`: stable source identifier.
- `DisplayName`: user-readable process or pod name.
- `ServiceName`: logical service name, for example `elsa-server`.
- `MachineName`: host machine name.
- `ProcessId`: process identifier when available.
- `PodName`, `Namespace`, `ContainerName`, `NodeName`: Kubernetes/container metadata when available.
- `StartedAt`: source start time when known.
- `LastSeen`: last event or heartbeat timestamp.
- `Status`: connected, stale, disconnected, unknown.

## ServerLogFilter

- `MinimumLevel`
- `Levels`
- `CategoryPrefix`
- `Text`
- `TenantId`
- `WorkflowDefinitionId`
- `WorkflowInstanceId`
- `TraceId`
- `CorrelationId`
- `SourceId`
- `From`
- `To`
- `Take`

## Invariants

- Events exposed to callers are always redacted.
- Buffer size is bounded by configuration.
- Every event has a source ID.
- Recent query `Take` is capped server-side.
- Source status is derived from provider state and `LastSeen`.
