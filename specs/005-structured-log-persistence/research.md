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

## Decision: Defer exporter/sink packages

**Rationale**: Vendor sinks and OTLP export are useful, but they are not required for the first customer persistence story. Keeping them out of this slice prevents the storage abstraction from being distorted by outbound shipping concerns.

**Alternatives considered**:

- Add OTLP logs exporter now: rejected until the future `Elsa.Diagnostics.OpenTelemetry` boundary is clearer.
- Add Logstash, Datadog, Splunk, Loki, or Seq now: rejected because they are better represented as sinks/exporters after the core store abstraction is stable.
