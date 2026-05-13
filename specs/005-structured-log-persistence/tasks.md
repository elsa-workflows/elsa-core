# Tasks: Structured Log Persistence

**Input**: Design documents from `/specs/005-structured-log-persistence/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification defines independent tests for in-memory compatibility, SQLite durability, filtering, migrations, queue overflow, timestamp storage, graceful shutdown flushing, and retention.

**Organization**: Tasks are grouped by user story so each increment can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks after prerequisites are satisfied.
- **[Story]**: User story label from [spec.md](./spec.md).
- Every task includes the primary file path to edit or create.

## Phase 1: Setup

**Purpose**: Add package skeletons, package references, and solution entries for relational and SQLite persistence.

- [ ] T001 [P] Create relational persistence project in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.csproj`.
- [ ] T002 [P] Create SQLite persistence project in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.csproj`.
- [ ] T003 [P] Create relational unit test project in `test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests.csproj`.
- [ ] T004 [P] Create SQLite integration test project in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests.csproj`.
- [ ] T005 Add new projects to `Elsa.sln`.
- [ ] T006 Add centrally managed package versions for FluentMigrator and SQLite dependencies in `Directory.Packages.props`.

---

## Phase 2: Foundational Storage Refactor

**Purpose**: Split store and live-feed concerns while preserving the current `IStructuredLogProvider` facade.

**Critical**: No SQLite story work should begin until the in-memory default still passes existing structured log tests.

- [ ] T007 [P] Add append-only sink contract in `src/modules/Elsa.Diagnostics.StructuredLogs/Contracts/IStructuredLogSink.cs`.
- [ ] T008 [P] Add queryable store contract in `src/modules/Elsa.Diagnostics.StructuredLogs/Contracts/IStructuredLogStore.cs`.
- [ ] T009 [P] Add live feed contract in `src/modules/Elsa.Diagnostics.StructuredLogs/Contracts/IStructuredLogLiveFeed.cs`.
- [ ] T010 Refactor in-memory recent-history behavior into `src/modules/Elsa.Diagnostics.StructuredLogs/Providers/InMemory/InMemoryStructuredLogStore.cs`.
- [ ] T011 Refactor in-memory subscriber behavior into `src/modules/Elsa.Diagnostics.StructuredLogs/Providers/InMemory/InMemoryStructuredLogLiveFeed.cs`.
- [ ] T012 Preserve `InMemoryStructuredLogProvider` compatibility facade in `src/modules/Elsa.Diagnostics.StructuredLogs/Providers/InMemory/InMemoryStructuredLogProvider.cs`.
- [ ] T013 Add composed provider facade in `src/modules/Elsa.Diagnostics.StructuredLogs/Services/DefaultStructuredLogProvider.cs`.
- [ ] T014 Update default DI registration in `src/modules/Elsa.Diagnostics.StructuredLogs/Extensions/ServiceCollectionExtensions.cs`.

**Checkpoint**: Existing structured logs behavior is preserved behind the new abstractions.

---

## Phase 3: User Story 1 - Preserve existing in-memory behavior (Priority: P1) MVP

**Goal**: Hosts that configure only `UseStructuredLogs` get the same bounded in-memory recent query, source listing, live streaming, redaction, and dropped-event behavior as before.

**Independent Test**: Enable structured logs without durable persistence, emit `ILogger` records, and verify recent queries, filters, source listing, live subscriptions, and dropped summaries.

### Tests for User Story 1

- [ ] T015 [P] [US1] Add default registration compatibility tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/StructuredLogsStorageRegistrationTests.cs`.
- [ ] T016 [P] [US1] Update in-memory recent query tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/InMemory/InMemoryStructuredLogProviderTests.cs`.
- [ ] T017 [P] [US1] Update in-memory source listing tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/InMemory/InMemoryStructuredLogProviderSourceTests.cs`.
- [ ] T018 [P] [US1] Update live dropped-event tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/InMemory/InMemoryStructuredLogProviderTests.cs`.

### Implementation for User Story 1

- [ ] T019 [US1] Ensure REST endpoints continue using `IStructuredLogProvider` in `src/modules/Elsa.Diagnostics.StructuredLogs/Endpoints/StructuredLogs/Recent/Endpoint.cs`.
- [ ] T020 [US1] Ensure source endpoint continues using `IStructuredLogProvider` in `src/modules/Elsa.Diagnostics.StructuredLogs/Endpoints/StructuredLogs/Sources/Endpoint.cs`.
- [ ] T021 [US1] Ensure SignalR subscriptions stream through the composed provider in `src/modules/Elsa.Diagnostics.StructuredLogs/RealTime/StructuredLogSubscriptionManager.cs`.
- [ ] T022 [US1] Run `dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Elsa.Diagnostics.StructuredLogs.UnitTests.csproj`.

**Checkpoint**: User Story 1 is fully functional and independently testable.

---

## Phase 4: User Story 2 - Enable durable SQLite structured log storage (Priority: P2)

**Goal**: Hosts can opt into SQLite storage, run migrations by default, persist redacted structured logs across restarts, query persisted records, flush queued writes on graceful shutdown, and configure retention explicitly.

**Independent Test**: Configure SQLite storage, emit events, flush or recreate services with the same database file, and verify persisted recent queries, filters, migration behavior, queue overflow behavior, timestamp storage, and retention behavior.

### Tests for User Story 2

- [ ] T023 [P] [US2] Add SQLite migration-from-empty-database test in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogMigrationTests.cs`.
- [ ] T024 [P] [US2] Add SQLite persistence-across-provider-recreation test in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogStoreTests.cs`.
- [ ] T025 [P] [US2] Add SQLite filter coverage for level, category, source, workflow, correlation, trace, time, and limit in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogFilterTests.cs`.
- [ ] T026 [P] [US2] Add SQLite write queue flush and overflow tests in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogWriteQueueTests.cs`.
- [ ] T027 [P] [US2] Add SQLite startup migration opt-out tests in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogMigrationTests.cs`.
- [ ] T028 [P] [US2] Add SQLite ISO-8601 timestamp storage tests in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogTimestampTests.cs`.
- [ ] T029 [P] [US2] Add SQLite retention tests for opt-in cleanup and default no-delete behavior in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogRetentionTests.cs`.

### Implementation for User Story 2

- [ ] T030 [US2] Add relational record model in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Models/RelationalStructuredLogRecord.cs`.
- [ ] T031 [US2] Add relational persistence options in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Options/RelationalStructuredLogOptions.cs`.
- [ ] T032 [US2] Add relational connection factory contract in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Contracts/IRelationalStructuredLogConnectionFactory.cs`.
- [ ] T033 [US2] Add relational dialect contract in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Contracts/IRelationalStructuredLogDialect.cs`.
- [ ] T034 [US2] Add schema migrator contract in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Contracts/IStructuredLogSchemaMigrator.cs`.
- [ ] T035 [US2] Add SQL builder for inserts, filters, sources, and retention in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Services/RelationalStructuredLogSqlBuilder.cs`.
- [ ] T036 [US2] Add JSON/timestamp mapper in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Services/RelationalStructuredLogMapper.cs`.
- [ ] T037 [US2] Add FluentMigrator migration for structured log tables and indexes in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Migrations/M001_CreateStructuredLogTables.cs`.
- [ ] T038 [US2] Add relational structured log store in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Stores/RelationalStructuredLogStore.cs`.
- [ ] T039 [US2] Add bounded write buffer with graceful shutdown flush in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Services/StructuredLogWriteBuffer.cs`.
- [ ] T040 [US2] Add retention cleanup service in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Services/StructuredLogRetentionService.cs`.
- [ ] T041 [US2] Add relational service registration extensions in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Extensions/RelationalStructuredLogsServiceCollectionExtensions.cs`.
- [ ] T042 [US2] Add SQLite options in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Options/SqliteStructuredLogOptions.cs`.
- [ ] T043 [US2] Add SQLite connection factory in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Services/SqliteStructuredLogConnectionFactory.cs`.
- [ ] T044 [US2] Add SQLite dialect in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Services/SqliteStructuredLogDialect.cs`.
- [ ] T045 [US2] Add FluentMigrator runner integration in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Services/SqliteStructuredLogSchemaMigrator.cs`.
- [ ] T046 [US2] Add SQLite hosted startup service for migrations and cleanup in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Services/SqliteStructuredLogStartupService.cs`.
- [ ] T047 [US2] Add SQLite fluent configuration extension in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Extensions/SqliteStructuredLogsModuleExtensions.cs`.
- [ ] T048 [US2] Run `dotnet test test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests.csproj`.

**Checkpoint**: User Stories 1 and 2 work together with durable SQLite storage.

---

## Phase 5: User Story 3 - Keep relational persistence extensible (Priority: P3)

**Goal**: Future relational providers can reuse the relational store by supplying a connection factory, dialect, and FluentMigrator runner configuration without changing core structured logs or Studio contracts.

**Independent Test**: Review code boundaries and run relational tests that use fake dialect or connection services where practical.

### Tests for User Story 3

- [ ] T049 [P] [US3] Add relational SQL builder tests with a fake dialect in `test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/RelationalStructuredLogSqlBuilderTests.cs`.
- [ ] T050 [P] [US3] Add relational mapper tests for JSON and UTC ISO-8601 timestamps in `test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/RelationalStructuredLogMapperTests.cs`.
- [ ] T051 [P] [US3] Add migration metadata/version tests in `test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/StructuredLogMigrationTests.cs`.
- [ ] T052 [P] [US3] Add core boundary test proving `Elsa.Diagnostics.StructuredLogs` has no SQLite dependency in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/StructuredLogsStorageRegistrationTests.cs`.

### Implementation for User Story 3

- [ ] T053 [US3] Keep provider-specific SQL outside core module in `src/modules/Elsa.Diagnostics.StructuredLogs`.
- [ ] T054 [US3] Keep SQLite-specific services inside `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite`.
- [ ] T055 [US3] Add relational provider guidance in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/README.md`.
- [ ] T056 [US3] Run `dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests.csproj`.

**Checkpoint**: SQLite is the first relational provider, not a one-off persistence path.

---

## Phase 6: Documentation & Polish

**Purpose**: Update public docs, sample guidance, and validation.

- [ ] T057 [P] Update structured logs README with storage mode guidance in `src/modules/Elsa.Diagnostics.StructuredLogs/README.md`.
- [ ] T058 [P] Add SQLite persistence README in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/README.md`.
- [ ] T059 [P] Update Speckit quickstart implementation notes in `specs/005-structured-log-persistence/quickstart.md`.
- [ ] T060 Update sample host wiring only if a sample explicitly opts into SQLite in `src/apps/Elsa.Server.Web/Program.cs`.
- [ ] T061 Run `dotnet build src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj`.
- [ ] T062 Run `dotnet build src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.csproj`.
- [ ] T063 Run `dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Elsa.Diagnostics.StructuredLogs.UnitTests.csproj`.
- [ ] T064 Run `dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests.csproj`.
- [ ] T065 Run `dotnet test test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests.csproj`.
- [ ] T066 Run `rg "OpenTelemetry|OTLP|Datadog|Logstash|Splunk|Loki|Seq" src/modules/Elsa.Diagnostics.StructuredLogs* specs/005-structured-log-persistence` and confirm only out-of-scope documentation references remain.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user stories.
- **Phase 3 US1**: Depends on Phase 2; MVP.
- **Phase 4 US2**: Depends on Phase 2 and should follow US1 validation.
- **Phase 5 US3**: Depends on relational and SQLite code from US2.
- **Phase 6 Polish**: Depends on selected story implementation.

### User Story Dependencies

- **US1 (P1)**: First executable slice; preserves current behavior after storage refactor.
- **US2 (P2)**: Adds durable SQLite storage using the new storage boundary.
- **US3 (P3)**: Hardens provider boundaries for future relational stores.

### Parallel Opportunities

- T001 through T004 can run in parallel.
- T007 through T009 can run in parallel.
- US1 tests T015 through T018 can run in parallel after Phase 2.
- US2 tests T023 through T029 can run in parallel after project skeleton exists.
- US3 tests T049 through T052 can run in parallel after relational contracts exist.
- Documentation tasks T057 through T059 can run in parallel after implementation APIs settle.

## Parallel Example: User Story 2

```text
Task: "Add SQLite migration-from-empty-database test in test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogMigrationTests.cs"
Task: "Add SQLite persistence-across-provider-recreation test in test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogStoreTests.cs"
Task: "Add SQLite filter coverage for level, category, source, workflow, correlation, trace, time, and limit in test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogFilterTests.cs"
Task: "Add SQLite retention tests for opt-in cleanup and default no-delete behavior in test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/SqliteStructuredLogRetentionTests.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 only.
3. Run existing structured logs tests and ensure no behavior changed.
4. Continue to SQLite persistence after the storage boundary is stable.

### Incremental Delivery

1. US1: in-memory compatibility after storage refactor.
2. US2: durable SQLite persistence with migrations, queue behavior, timestamp storage, and retention.
3. US3: relational provider extensibility.
4. Polish: documentation, sample guidance, builds, and tests.

## Notes

- Do not implement OTLP or vendor sinks in this feature.
- Do not introduce EF Core for structured log persistence.
- Preserve redaction-before-storage.
- Keep Studio REST and SignalR contracts unchanged.
- Prefer small, explicit SQL over clever generic query abstraction unless implementation pressure proves otherwise.
