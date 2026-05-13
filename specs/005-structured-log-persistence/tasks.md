# Tasks: Structured Log Persistence

**Input**: Design documents from `/specs/005-structured-log-persistence/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification defines independent tests for in-memory compatibility, SQLite durability, filtering, migrations, and retention.

**Organization**: Tasks are grouped by user story so each increment can be validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks after prerequisites are satisfied.
- **[Story]**: User story label from [spec.md](./spec.md).
- Every task includes the primary file path to edit or create.

## Phase 1: Setup

**Purpose**: Add package skeletons, package references, and solution entries for relational and SQLite persistence.

- [ ] T001 Create `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.csproj`.
- [ ] T002 Create `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.csproj`.
- [ ] T003 Create `test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests.csproj`.
- [ ] T004 Create `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests.csproj`.
- [ ] T005 Add new projects to `Elsa.sln`.
- [ ] T006 Add centrally managed package versions for FluentMigrator, SQLite provider, and Dapper if used in `Directory.Packages.props`.

---

## Phase 2: Foundational Storage Refactor

**Purpose**: Split store/live-feed concerns while preserving existing `IStructuredLogProvider` behavior.

**Critical**: No SQLite work should begin until in-memory behavior is preserved behind the new boundaries.

- [ ] T007 Add `IStructuredLogSink` in `src/modules/Elsa.Diagnostics.StructuredLogs/Contracts/IStructuredLogSink.cs`.
- [ ] T008 Add `IStructuredLogStore` in `src/modules/Elsa.Diagnostics.StructuredLogs/Contracts/IStructuredLogStore.cs`.
- [ ] T009 Add `IStructuredLogLiveFeed` in `src/modules/Elsa.Diagnostics.StructuredLogs/Contracts/IStructuredLogLiveFeed.cs`.
- [ ] T010 Refactor `InMemoryStructuredLogProvider` storage behavior into in-memory store/live-feed components under `src/modules/Elsa.Diagnostics.StructuredLogs/Providers/InMemory`.
- [ ] T011 Add a composed provider facade in `src/modules/Elsa.Diagnostics.StructuredLogs/Services/DefaultStructuredLogProvider.cs`.
- [ ] T012 Update service registration in `src/modules/Elsa.Diagnostics.StructuredLogs/Extensions/ServiceCollectionExtensions.cs`.
- [ ] T013 Update existing unit tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests` for the new in-memory component names.

**Checkpoint**: Existing structured logs tests pass with the default in-memory storage.

---

## Phase 3: User Story 1 - Preserve existing in-memory behavior (Priority: P1) MVP

**Goal**: No-configuration structured logs behave exactly as before from the host, REST, SignalR, and Studio perspective.

**Independent Test**: Enable structured logs without durable storage and verify capture, recent query, filtering, sources, live streaming, and dropped summaries.

### Tests for User Story 1

- [ ] T014 [P] [US1] Add compatibility tests for default store registration in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests`.
- [ ] T015 [P] [US1] Preserve or update in-memory recent query tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/InMemory`.
- [ ] T016 [P] [US1] Preserve or update live dropped-event tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/InMemory`.

### Implementation for User Story 1

- [ ] T017 [US1] Ensure `UseStructuredLogs` defaults to bounded in-memory store/live feed in `src/modules/Elsa.Diagnostics.StructuredLogs/Extensions/ServiceCollectionExtensions.cs`.
- [ ] T018 [US1] Ensure REST endpoints continue to depend on `IStructuredLogProvider` in `src/modules/Elsa.Diagnostics.StructuredLogs/Endpoints/StructuredLogs`.
- [ ] T019 [US1] Ensure `StructuredLogSubscriptionManager` can stream from the composed provider in `src/modules/Elsa.Diagnostics.StructuredLogs/RealTime/StructuredLogSubscriptionManager.cs`.

**Checkpoint**: User Story 1 is releasable by itself.

---

## Phase 4: User Story 2 - Enable durable SQLite structured log storage (Priority: P2)

**Goal**: Hosts can opt into SQLite storage and retain structured log events across restarts.

**Independent Test**: Configure SQLite, emit events, recreate the service provider with the same database, and query persisted events.

### Tests for User Story 2

- [ ] T020 [P] [US2] Add SQLite migration-from-empty-database test in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests`.
- [ ] T021 [P] [US2] Add SQLite persistence-across-provider-recreation test in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests`.
- [ ] T022 [P] [US2] Add SQLite filter coverage for level/category/source/workflow/correlation/trace/time/limit in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests`.
- [ ] T023 [P] [US2] Add SQLite retention cleanup tests in `test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests`.

### Implementation for User Story 2

- [ ] T024 [US2] Add relational record model in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Models/RelationalStructuredLogRecord.cs`.
- [ ] T025 [US2] Add relational connection factory contract in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Contracts`.
- [ ] T026 [US2] Add relational dialect contract and base SQL builder in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Services`.
- [ ] T027 [US2] Add FluentMigrator migration for structured log tables/indexes in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Migrations`.
- [ ] T028 [US2] Add relational structured log store implementation in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Stores`.
- [ ] T029 [US2] Add retention options and cleanup service in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational`.
- [ ] T030 [US2] Add SQLite connection factory in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Services`.
- [ ] T031 [US2] Add SQLite dialect in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Services`.
- [ ] T032 [US2] Add SQLite FluentMigrator runner registration in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite`.
- [ ] T033 [US2] Add SQLite fluent configuration extension in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Extensions`.
- [ ] T034 [US2] Ensure queued/batched writes flush during shutdown in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/Services`.

**Checkpoint**: User Stories 1 and 2 work together with durable SQLite storage.

---

## Phase 5: User Story 3 - Keep relational persistence extensible (Priority: P3)

**Goal**: Future relational providers can be added by supplying provider services, not by rewriting structured log capture or Studio contracts.

**Independent Test**: Review code boundaries and tests that use fake dialect/connection services where practical.

### Tests for User Story 3

- [ ] T035 [P] [US3] Add relational SQL builder tests with a fake dialect in `test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests`.
- [ ] T036 [P] [US3] Add registration boundary tests proving core module has no SQLite dependency in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests`.
- [ ] T037 [P] [US3] Add migration metadata/version tests in `test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests`.

### Implementation for User Story 3

- [ ] T038 [US3] Keep provider-specific SQL out of `src/modules/Elsa.Diagnostics.StructuredLogs`.
- [ ] T039 [US3] Keep SQLite-specific services inside `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite`.
- [ ] T040 [US3] Document future SQL Server/PostgreSQL provider requirements in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/README.md`.

**Checkpoint**: SQLite is the first relational provider, not a one-off persistence path.

---

## Phase 6: Documentation & Polish

**Purpose**: Update public docs, sample guidance, and validation.

- [ ] T041 Update structured logs README with storage mode guidance in `src/modules/Elsa.Diagnostics.StructuredLogs/README.md`.
- [ ] T042 Add relational persistence README in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational/README.md`.
- [ ] T043 Add SQLite persistence README in `src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/README.md`.
- [ ] T044 Update sample host wiring only if a sample explicitly opts into SQLite in `src/apps/Elsa.Server.Web/Program.cs`.
- [ ] T045 Run `dotnet build src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj`.
- [ ] T046 Run `dotnet build src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.csproj`.
- [ ] T047 Run `dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Elsa.Diagnostics.StructuredLogs.UnitTests.csproj`.
- [ ] T048 Run `dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests/Elsa.Diagnostics.StructuredLogs.Persistence.Relational.UnitTests.csproj`.
- [ ] T049 Run `dotnet test test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests.csproj`.
- [ ] T050 Run `rg "OpenTelemetry|OTLP|Datadog|Logstash|Splunk|Loki|Seq" src/modules/Elsa.Diagnostics.StructuredLogs* specs/005-structured-log-persistence` and confirm only out-of-scope documentation references remain.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks SQLite work.
- **Phase 3 US1**: Depends on Phase 2; MVP.
- **Phase 4 US2**: Depends on Phase 2 and should follow US1 validation.
- **Phase 5 US3**: Depends on relational code from US2.
- **Phase 6 Polish**: Depends on selected story implementation.

### User Story Dependencies

- **US1 (P1)**: First executable slice; preserves current behavior.
- **US2 (P2)**: Adds durable SQLite storage using the new storage boundary.
- **US3 (P3)**: Hardens provider boundaries for future relational stores.

### Parallel Opportunities

- T001 through T004 can run in parallel.
- T007 through T009 can run in parallel.
- US1 tests T014 through T016 can run in parallel after Phase 2.
- US2 tests T020 through T023 can run in parallel after SQLite skeleton exists.
- US3 tests T035 through T037 can run in parallel after relational contracts exist.
- Documentation tasks T041 through T043 can run in parallel after implementation APIs settle.

## Parallel Example: User Story 2

```text
Task: "Add SQLite migration-from-empty-database test in test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests"
Task: "Add SQLite persistence-across-provider-recreation test in test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests"
Task: "Add SQLite filter coverage for level/category/source/workflow/correlation/trace/time/limit in test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests"
Task: "Add SQLite retention cleanup tests in test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 only.
3. Run existing structured logs tests and ensure no behavior changed.
4. Continue to SQLite persistence after the storage boundary is stable.

### Incremental Delivery

1. US1: in-memory compatibility after storage refactor.
2. US2: durable SQLite persistence with migrations and retention.
3. US3: relational provider extensibility.
4. Polish: documentation, sample guidance, builds, and tests.

## Notes

- Do not implement OTLP or vendor sinks in this feature.
- Do not introduce EF Core for structured log persistence.
- Preserve redaction-before-storage.
- Keep Studio REST and SignalR contracts unchanged.
- Prefer small, boring SQL over clever generic query abstraction unless implementation pressure proves otherwise.
