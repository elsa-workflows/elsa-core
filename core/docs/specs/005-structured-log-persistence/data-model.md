# Data Model: Structured Log Persistence

## StructuredLogStore

Queryable storage boundary for redacted structured log events.

- Appends redacted `StructuredLogEvent` values.
- Queries recent events using `StructuredLogFilter`.
- Lists known `StructuredLogSource` values.
- Does not own live subscriber backpressure behavior.

## StructuredLogLiveFeed

Runtime live delivery boundary for new events.

- Publishes new redacted events to live subscribers.
- Applies subscription filters.
- Reports dropped-event summaries when subscriber queues overflow.
- Does not guarantee durability.

## StructuredLogSink

Append-only boundary for destinations that receive structured log events.

- Accepts single events or batches.
- Does not have to support queries.
- Used internally by stores and later exporter packages.

## RelationalStructuredLogRecord

Relational database representation of `StructuredLogEvent`.

- `Id`: unique event identifier.
- `Sequence`: monotonic sequence assigned by the logger path.
- `Timestamp`: original event timestamp stored as UTC ISO-8601 text.
- `ReceivedAt`: backend receive timestamp stored as UTC ISO-8601 text.
- `Level`: structured log level stored in a queryable representation.
- `Category`: logger category.
- `EventId`: numeric Microsoft logging event ID.
- `EventName`: optional Microsoft logging event name.
- `Message`: rendered message.
- `MessageTemplate`: original template from `{OriginalFormat}` when available.
- `ExceptionJson`: serialized `StructuredLogException`.
- `ScopesJson`: serialized scope key/value values.
- `PropertiesJson`: serialized structured property key/value values.
- `TraceId`, `SpanId`: active `Activity` identifiers.
- `CorrelationId`: correlation value when available.
- `TenantId`, `WorkflowDefinitionId`, `WorkflowInstanceId`: Elsa context values when available.
- `SourceId`: producing source identifier.

Recommended indexes:

- `ReceivedAt`
- `Timestamp`
- `Level`
- `Category`
- `SourceId`
- `TenantId`
- `WorkflowDefinitionId`
- `WorkflowInstanceId`
- `CorrelationId`
- `TraceId`
- Composite index for recent ordering, such as `ReceivedAt`, `Sequence`, `Id`

## RelationalStructuredLogSourceRecord

Optional relational representation of `StructuredLogSource` when the store persists sources separately.

- `Id`: stable source identifier.
- `Name`: display name.
- `MachineName`, `ProcessId`, `ProcessName`: process metadata.
- `PodName`, `Namespace`, `ContainerName`, `NodeName`: Kubernetes/container metadata.
- `StartedAt`, `LastSeen`: lifecycle timestamps.
- `Status`: source status.

## StructuredLogRetentionOptions

Settings controlling durable store growth.

- `MaxAge`: optional maximum event age.
- `MaxRows`: optional maximum number of retained rows.
- `CleanupInterval`: background cleanup interval.
- `RunCleanupOnStartup`: whether to clean immediately after migrations.
- Default behavior: delete no records unless `MaxAge`, `MaxRows`, or both are configured.

## StructuredLogWriteQueueOptions

Settings controlling SQLite write buffering.

- `Capacity`: maximum queued events waiting to be written.
- `BatchSize`: maximum events written in one flush.
- `FlushInterval`: maximum time between background flushes.
- `ShutdownFlushTimeout`: maximum graceful shutdown flush duration.
- `DroppedWriteCount`: reported count of events dropped because the queue was full.
- Overflow behavior: drop newly received events when the queue is full; never block logging calls or allocate unbounded memory.

## RelationalStructuredLogDialect

Provider-specific SQL behavior.

- Identifier quoting.
- Parameter prefix behavior when needed.
- Limit/take syntax.
- Timestamp storage and comparison behavior.
- Free-text predicate construction.
- Provider name for FluentMigrator database branching.

## StructuredLogMigrationRunner

Schema management service.

- Runs FluentMigrator migrations for the configured provider.
- Creates the structured log schema from an empty database.
- Applies future upgrades in version order.
- Runs on SQLite startup by default and can be disabled.
- Documents multi-instance startup locking constraints for shared database providers.
