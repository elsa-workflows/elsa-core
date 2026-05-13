# Feature Specification: Structured Log Persistence

**Feature Branch**: `005-structured-log-persistence`  
**Created**: 2026-05-12  
**Status**: Draft  
**Input**: User description: "Make structured log storage a pluggable concern. Keep in-memory support, add an easy durable SQLite storage story first, use FluentMigrator for schema management, and defer OpenTelemetry/exporter scope."

## Clarifications

### Session 2026-05-12

- Q: Should this feature add vendor sinks such as Logstash, Datadog, Splunk, Loki, or Seq now? -> A: No. Keep the first slice small.
- Q: Should this feature add an OTLP logs exporter now? -> A: No. Defer OpenTelemetry until the diagnostics OpenTelemetry boundary is clearer.
- Q: What storage providers should exist initially? -> A: Preserve in-memory storage and add opt-in SQLite durable storage.
- Q: Should SQLite be implemented as a one-off store? -> A: No. Implement it as the first relational provider so SQL Server, PostgreSQL, MySQL, and similar providers can be added later.
- Q: Should EF Core be used for persistence? -> A: No. Avoid EF Core and per-provider EF migrations for this feature.
- Q: Should FluentMigrator manage schema creation and upgrades? -> A: Yes. Use FluentMigrator for schema versioning and migration execution; use explicit SQL/Dapper-style access for the hot storage path.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Preserve the existing in-memory structured logs behavior (Priority: P1)

An Elsa host developer enables structured logs without configuring durable persistence and gets the same bounded in-memory recent history and live streaming behavior as today.

**Why this priority**: The storage refactor must not regress the current module. In-memory remains the zero-configuration default and the fastest validation path.

**Independent Test**: Enable `UseStructuredLogs` with no persistence options, emit several `ILogger` events, and verify recent queries, filtering, source listing, live subscriptions, redaction, and dropped-event summaries continue to work.

**Acceptance Scenarios**:

1. **Given** no durable store is configured, **When** structured logs are enabled, **Then** the module uses bounded in-memory storage by default.
2. **Given** recent logs are queried through the existing REST endpoint, **When** events have been captured, **Then** the endpoint returns filtered recent events using the same contract as before.
3. **Given** Studio subscribes over SignalR, **When** new matching events are emitted, **Then** live events and dropped-event summaries continue to stream.

---

### User Story 2 - Enable durable SQLite structured log storage (Priority: P2)

An Elsa host developer opts into SQLite persistence so structured log entries survive process restarts and can be queried by Studio after restart.

**Why this priority**: Customers need an easy persistence story without standing up a separate database service or observability stack.

**Independent Test**: Configure SQLite storage, emit structured log entries, restart or recreate the service provider with the same database file, and verify recent queries return the previously written events.

**Acceptance Scenarios**:

1. **Given** SQLite storage is configured with a database file, **When** structured log events are captured, **Then** the events are written durably to SQLite.
2. **Given** the host restarts with the same SQLite database, **When** recent structured logs are queried, **Then** persisted events are returned according to the filter.
3. **Given** retention settings are configured, **When** cleanup runs, **Then** old or excess records are deleted without breaking recent queries.
4. **Given** schema migrations have not run, **When** the SQLite provider starts, **Then** FluentMigrator creates or upgrades the structured log schema.

---

### User Story 3 - Keep relational persistence extensible for future databases (Priority: P3)

An Elsa maintainer can add SQL Server, PostgreSQL, MySQL, or another relational provider later without rewriting the structured logs module or changing Studio contracts.

**Why this priority**: SQLite should prove the relational persistence model, not become a dead-end implementation.

**Independent Test**: Review the relational contracts, dialect hooks, migrations, and SQLite package to confirm provider-specific behavior is isolated from the core structured logs module.

**Acceptance Scenarios**:

1. **Given** a future database provider is added, **When** it supplies a connection factory, SQL dialect, and FluentMigrator runner registration, **Then** it can reuse the shared relational store implementation.
2. **Given** a provider needs database-specific DDL, **When** migrations run, **Then** the migration can branch by database while keeping a shared migration version.
3. **Given** Studio uses existing structured logs REST and SignalR contracts, **When** storage changes from in-memory to SQLite, **Then** Studio requires no API changes.

### Edge Cases

- Durable writes must not block `ILogger` callers on slow disk I/O.
- A host can emit logs before migrations complete if startup ordering is wrong.
- Multiple app instances can start against a shared future relational database and attempt migrations concurrently.
- SQLite file paths can be missing, relative, or point to directories without write permissions.
- A process can crash after events enter a background queue but before they are flushed.
- Free-text filtering over JSON fields can be provider-specific and initially approximate.
- Retention cleanup can race with recent queries or live subscriptions.
- Schema changes must preserve already persisted log events where practical.

## Requirements *(mandatory)*

### Functional Requirements

**Storage abstraction**

- **FR-001**: The structured logs module MUST separate queryable storage from live streaming and capture/provider facade responsibilities.
- **FR-002**: The existing `IStructuredLogProvider` contract MAY remain as the facade used by REST endpoints and SignalR, but storage-specific responsibilities MUST be represented by a replaceable store abstraction.
- **FR-003**: The storage abstraction MUST support appending redacted `StructuredLogEvent` values, querying recent events by `StructuredLogFilter`, and listing structured log sources.
- **FR-004**: Live streaming MUST remain available when durable storage is configured.
- **FR-005**: Redaction MUST continue to happen before any event reaches in-memory storage, SQLite storage, or live subscribers.

**In-memory default**

- **FR-006**: The module MUST keep bounded in-memory storage as the default when no durable storage provider is configured.
- **FR-007**: The in-memory implementation MUST preserve existing recent query, source listing, subscription, dropped-event summary, capacity, and filtering behavior.

**SQLite durable storage**

- **FR-008**: The feature MUST add an opt-in SQLite structured log persistence provider.
- **FR-009**: SQLite storage MUST persist structured log events across process restarts.
- **FR-010**: SQLite storage MUST support the existing `StructuredLogFilter` fields used by the REST recent-log endpoint.
- **FR-011**: SQLite storage MUST persist scalar filter fields as queryable columns and persist exception, scopes, and properties as serialized JSON.
- **FR-012**: SQLite storage MUST provide configurable retention by maximum age and/or maximum row count.
- **FR-013**: SQLite writes SHOULD be batched or queued so logging calls do not synchronously wait on disk I/O.
- **FR-014**: SQLite storage MUST flush queued events during graceful shutdown where possible.

**Relational extensibility**

- **FR-015**: Shared relational persistence code MUST be reusable by future SQL Server, PostgreSQL, MySQL, and similar providers.
- **FR-016**: Provider-specific SQL syntax MUST be isolated behind dialect or provider services.
- **FR-017**: The first SQLite provider MUST not introduce SQLite-only assumptions into the core structured logs module.
- **FR-018**: Future relational providers MUST be able to supply their own connection factory, dialect, and migration runner configuration without changing REST, SignalR, or Studio contracts.

**Schema management**

- **FR-019**: Relational schema creation and upgrades MUST use FluentMigrator.
- **FR-020**: Migrations MUST be versioned and idempotently executable by the host.
- **FR-021**: Provider packages MUST register only the FluentMigrator runner dependencies they need.
- **FR-022**: The feature MUST document startup migration behavior and the multi-instance locking consideration for future shared database providers.

**Configuration and safety**

- **FR-023**: Host configuration MUST make the active storage mode explicit when SQLite is selected.
- **FR-024**: The existing no-configuration in-memory setup MUST continue to work.
- **FR-025**: The feature MUST document that OpenTelemetry/exporter sinks are deferred and out of scope for this persistence slice.

### Key Entities *(include if feature involves data)*

- **Structured Log Store**: Queryable storage for redacted structured log events and sources.
- **Structured Log Live Feed**: Runtime stream of new events and dropped-event summaries for SignalR subscribers.
- **Relational Structured Log Record**: Database representation of a `StructuredLogEvent` with indexed scalar columns and JSON payload columns.
- **Relational Storage Dialect**: Provider-specific SQL and parameter behavior needed by the shared relational store.
- **Structured Log Migration Runner**: FluentMigrator-based schema creation and upgrade service.
- **Structured Log Retention Policy**: Settings and cleanup behavior that bound durable storage growth.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Existing structured logs unit and integration tests continue to pass with the in-memory default.
- **SC-002**: New tests verify SQLite persistence survives service-provider or process recreation with the same database file.
- **SC-003**: New tests verify SQLite recent queries honor level, category, source, workflow, correlation, trace/span, time range, and limit filters.
- **SC-004**: New tests verify FluentMigrator creates the SQLite schema from an empty database.
- **SC-005**: New tests verify retention cleanup removes expired or excess records.
- **SC-006**: Documentation shows both zero-configuration in-memory setup and opt-in SQLite setup.
- **SC-007**: The core structured logs module does not reference SQLite-specific types.

## Assumptions

- The structured logs module from `004-diagnostics-structured-logs` is available and remains the owning diagnostics module.
- SQLite is the only durable provider implemented in this feature.
- Future relational providers can be added in separate specs and packages.
- OpenTelemetry logs export, vendor sinks, raw console streaming, and trace/metric exploration remain out of scope.
- The implementation can introduce new package dependencies such as FluentMigrator and Dapper where appropriate, with versions managed centrally.
