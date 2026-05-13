# Research: Structured Log Persistence

## Decision: Preserve in-memory storage as the default

**Rationale**: The existing bounded in-memory provider is the current structured logs behavior and has the lowest operational burden. It should remain the zero-configuration path for development, tests, and hosts that only need live diagnostics.

**Alternatives considered**:

- Make SQLite the default: rejected because it adds disk, migration, and retention behavior to hosts that did not ask for durability.
- Remove in-memory storage: rejected because it would break the existing feature and complicate local development.

## Decision: Split store and live-feed responsibilities from the provider facade

**Rationale**: `IStructuredLogProvider` currently combines append, recent query, source listing, and live subscription behavior. Durable persistence needs queryable storage, while SignalR needs live delivery. Splitting these concerns makes storage replaceable without forcing every store to implement subscriber queue behavior.

**Alternatives considered**:

- Keep only `IStructuredLogProvider`: rejected because durable stores and vendor sinks would inherit unrelated live streaming responsibilities.
- Replace `IStructuredLogProvider` entirely: rejected because endpoints and SignalR already depend on it and the existing contract is a useful facade.

## Decision: Add SQLite as the first durable store

**Rationale**: SQLite gives customers an easy persistence story with no external database service. It is a good first step for single-node Elsa hosts and a useful proving ground for a relational store contract.

**Alternatives considered**:

- Add OTLP first: rejected because OpenTelemetry scope should be handled deliberately by a later diagnostics/observability feature.
- Add SQL Server or PostgreSQL first: rejected because they raise deployment requirements for the first persistence story.
- Use Parquet as the default durable format: rejected because Parquet is better suited to archive and analytics exports than hot append/query/live operational diagnostics.

## Decision: Build SQLite on shared relational persistence

**Rationale**: Customers are likely to ask for SQL Server, PostgreSQL, MySQL, or other relational providers later. Shared relational store logic keeps the SQLite provider from becoming a one-off and reduces future provider work.

**Alternatives considered**:

- SQLite-only implementation: rejected because it would create duplicate work when the next relational provider is requested.
- Fully generic provider with no dialect abstraction: rejected because limit syntax, identifiers, JSON behavior, date handling, and migration details vary by database.

## Decision: Use FluentMigrator for schema management

**Rationale**: FluentMigrator provides versioned migrations without adopting EF Core as the persistence layer. It supports several database providers and allows shared migrations with provider-specific branches when needed.

**Alternatives considered**:

- EF Core migrations: rejected because maintaining migrations per provider is a burden and EF runtime features are not needed for append/query/delete log storage.
- Hand-rolled `CREATE TABLE IF NOT EXISTS`: rejected because it lacks a clean schema upgrade path once fields or indexes evolve.
- External SQL scripts only: rejected because hosts need an easy in-process migration story, especially for SQLite.

## Decision: Use explicit SQL/Dapper-style data access for runtime operations

**Rationale**: Structured log storage needs append, filtered query, source list, and retention delete operations. These are predictable SQL operations and do not need change tracking, navigation properties, or LINQ translation. Dapper can reduce mapping boilerplate while keeping SQL explicit.

**Alternatives considered**:

- EF Core runtime access: rejected for the same reasons as EF migrations.
- A generic query-builder library: deferred because current filters are simple enough to compose directly with a small dialect service.

## Decision: Use async batched SQLite writes with graceful-shutdown flush

**Rationale**: Structured log capture must not make `ILogger` callers wait on disk I/O. A background writer preserves the diagnostics feature's low-latency capture path while graceful shutdown flushes queued events where possible.

**Alternatives considered**:

- Synchronous durable writes per event: rejected because slow disk I/O would directly affect application logging paths.
- Configurable synchronous durability mode: deferred because the first persistence story prioritizes simple operational diagnostics over no-loss audit logging.

## Decision: Use a bounded drop-newest SQLite write queue

**Rationale**: The write queue must not grow without bound during logging spikes or disk stalls. Dropping newly received events keeps already queued events stable, keeps memory bounded, and makes loss visible through dropped-write counts.

**Alternatives considered**:

- Block logging when the queue is full: rejected because persistence backpressure would slow application code.
- Drop oldest queued events: rejected because it makes the batch already accepted for persistence less predictable.
- Use an unbounded queue: rejected because it can turn a logging spike into process memory pressure.

## Decision: Run SQLite migrations on startup by default

**Rationale**: The SQLite provider should provide an easy durable logging story. Startup migrations make first-run setup simple, while an opt-out lets advanced hosts prepare schemas separately.

**Alternatives considered**:

- Require explicit migration calls: rejected because it weakens the easy persistence story.
- Run migrations only in development: rejected because SQLite is expected to be useful in small production hosts too.

## Decision: Store SQLite timestamps as UTC ISO-8601 text

**Rationale**: UTC ISO-8601 text is SQLite-friendly, human-readable, sortable when normalized, and avoids provider-specific date/time behavior in the first durable provider.

**Alternatives considered**:

- Unix epoch milliseconds: rejected because it is less inspectable and loses sub-millisecond detail.
- Unix epoch ticks or nanoseconds: rejected because it is less readable and still requires conversion for most manual inspection.

## Decision: Make retention opt-in

**Rationale**: Durable storage should not delete customer logs unexpectedly. Hosts can configure maximum age, maximum rows, or both when they want bounded durable storage.

**Alternatives considered**:

- Default 14-day and 250,000-row retention: rejected because default deletion can surprise operators.
- Default maximum rows only: rejected because any default deletion policy still needs explicit customer intent.

## Decision: Defer exporter/sink packages

**Rationale**: Vendor sinks and OTLP export are useful, but they are not required for the first customer persistence story. Keeping them out of this slice prevents the storage abstraction from being distorted by outbound shipping concerns.

**Alternatives considered**:

- Add OTLP logs exporter now: rejected until the future `Elsa.Diagnostics.OpenTelemetry` boundary is clearer.
- Add Logstash, Datadog, Splunk, Loki, or Seq now: rejected because they are better represented as sinks/exporters after the core store abstraction is stable.
