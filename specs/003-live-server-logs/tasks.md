# Tasks: Live Server Log Streaming

**Input**: Design documents from `/specs/003-live-server-logs/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification defines independent tests and the plan calls out unit/integration coverage for security, redaction, bounded buffers, filtering, and SignalR flow.

**Organization**: Tasks are grouped by user story to keep each increment independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks in the same phase after prerequisites are satisfied.
- **[Story]**: User story label from [spec.md](./spec.md).
- Every task includes the primary file path to edit or create.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the diagnostics module and test project shells.

- [X] T001 Create `src/modules/Elsa.Diagnostics/Elsa.Diagnostics.csproj` with target/import settings matching existing Elsa modules.
- [X] T002 Add `src/modules/Elsa.Diagnostics/Elsa.Diagnostics.csproj` to `Elsa.sln`.
- [ ] T003 [P] Create `test/unit/Elsa.Diagnostics.UnitTests/Elsa.Diagnostics.UnitTests.csproj` with xUnit dependencies matching adjacent unit test projects.
- [ ] T004 [P] Create `test/integration/Elsa.Diagnostics.IntegrationTests/Elsa.Diagnostics.IntegrationTests.csproj` with test host dependencies matching adjacent integration test projects.
- [ ] T005 Add diagnostics test projects to `Elsa.sln`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared contracts, models, options, and registration surfaces required by all stories.

**Critical**: No user story work should begin until this phase is complete.

- [X] T006 Create `src/modules/Elsa.Diagnostics/Models/ServerLogLevel.cs` for log level values exposed over API contracts.
- [X] T007 [P] Create `src/modules/Elsa.Diagnostics/Models/ServerLogException.cs` for redacted exception summary/detail.
- [X] T008 [P] Create `src/modules/Elsa.Diagnostics/Models/ServerLogSource.cs` for process, machine, pod, container, namespace, node, and health metadata.
- [X] T009 [P] Create `src/modules/Elsa.Diagnostics/Models/ServerLogEvent.cs` with fields from `data-model.md`.
- [X] T010 [P] Create `src/modules/Elsa.Diagnostics/Models/ServerLogFilter.cs` with recent/live filter fields.
- [X] T011 [P] Create `src/modules/Elsa.Diagnostics/Models/ServerLogDroppedEventSummary.cs` for dropped-event reporting.
- [X] T012 Create `src/modules/Elsa.Diagnostics/Contracts/IServerLogProvider.cs` from `contracts/provider-contract.md`.
- [X] T013 [P] Create `src/modules/Elsa.Diagnostics/Contracts/IServerLogRedactor.cs` for message, exception, scope, and property redaction.
- [X] T014 [P] Create `src/modules/Elsa.Diagnostics/Contracts/IServerLogSourceRegistry.cs` for source identity and health updates.
- [X] T015 Create `src/modules/Elsa.Diagnostics/Options/ServerLogStreamingOptions.cs` with bounded defaults for recent capacity, channel capacity, query limit, heartbeat timeout, and redaction rules.
- [X] T016 Create `src/modules/Elsa.Diagnostics/Features/ServerLogStreamingFeature.cs` that registers options and shared diagnostics services.
- [X] T017 Create `src/modules/Elsa.Diagnostics/Extensions/ModuleExtensions.cs` with `UseServerLogStreaming`.
- [X] T018 Create `src/modules/Elsa.Diagnostics/Extensions/ApplicationBuilderExtensions.cs` with placeholder hub/endpoint mapping for later phases.

**Checkpoint**: Module shell, contracts, models, options, and registration APIs compile.

---

## Phase 3: User Story 1 - Tail live server logs from Studio (Priority: P1) MVP

**Goal**: An authorized developer/operator can request recent log events and receive live `ILogger` events over SignalR.

**Independent Test**: Enable the feature, emit multiple `ILogger` events, verify recent backfill and live events arrive with timestamp, level, category, message, exception, and source metadata.

### Tests for User Story 1

- [ ] T019 [P] [US1] Add ring buffer capacity and ordering tests in `test/unit/Elsa.Diagnostics.UnitTests/InMemory/RingBufferTests.cs`.
- [ ] T020 [P] [US1] Add in-memory provider recent/live tests in `test/unit/Elsa.Diagnostics.UnitTests/InMemory/InMemoryServerLogProviderTests.cs`.
- [ ] T021 [P] [US1] Add logger provider capture tests in `test/unit/Elsa.Diagnostics.UnitTests/Logging/ServerLogLoggerProviderTests.cs`.
- [ ] T022 [P] [US1] Add recent endpoint integration test in `test/integration/Elsa.Diagnostics.IntegrationTests/ServerLogsRecentEndpointTests.cs`.
- [ ] T023 [P] [US1] Add SignalR live subscription integration test in `test/integration/Elsa.Diagnostics.IntegrationTests/ServerLogsHubTests.cs`.

### Implementation for User Story 1

- [X] T024 [P] [US1] Implement bounded storage in `src/modules/Elsa.Diagnostics/Providers/InMemory/RingBuffer.cs`.
- [X] T025 [US1] Implement recent query and live subscribe/publish flow in `src/modules/Elsa.Diagnostics/Providers/InMemory/InMemoryServerLogProvider.cs`.
- [X] T026 [P] [US1] Implement source metadata defaults in `src/modules/Elsa.Diagnostics/Services/ServerLogSourceRegistry.cs`.
- [X] T027 [US1] Implement `ILoggerProvider` capture in `src/modules/Elsa.Diagnostics/Logging/ServerLogLoggerProvider.cs`.
- [X] T028 [US1] Implement logger event creation and recursion guard in `src/modules/Elsa.Diagnostics/Logging/ServerLogLogger.cs`.
- [X] T029 [US1] Register the logger provider and in-memory provider in `src/modules/Elsa.Diagnostics/Features/ServerLogStreamingFeature.cs`.
- [X] T030 [US1] Implement recent-log endpoint in `src/modules/Elsa.Diagnostics/Endpoints/ServerLogs/Recent/Endpoint.cs`.
- [X] T031 [US1] Implement SignalR hub subscribe/unsubscribe/live event methods in `src/modules/Elsa.Diagnostics/RealTime/ServerLogsHub.cs`.
- [X] T032 [US1] Map `/elsa/hubs/server-logs` in `src/modules/Elsa.Diagnostics/Extensions/ApplicationBuilderExtensions.cs`.

**Checkpoint**: User Story 1 is functional and testable with recent backfill plus live stream in a single-process backend.

---

## Phase 4: User Story 2 - Filter and secure operational logs (Priority: P2)

**Goal**: Authorized operators can filter logs while unauthorized callers are rejected and secrets are redacted before events leave the server.

**Independent Test**: Connect with and without `read:server-logs`, emit sensitive structured events, and verify authorization, redaction, and filter behavior.

### Tests for User Story 2

- [ ] T033 [P] [US2] Add filter predicate tests in `test/unit/Elsa.Diagnostics.UnitTests/Filtering/ServerLogFilterTests.cs`.
- [ ] T034 [P] [US2] Add redaction tests in `test/unit/Elsa.Diagnostics.UnitTests/Redaction/ServerLogRedactorTests.cs`.
- [ ] T035 [P] [US2] Add recent endpoint authorization tests in `test/integration/Elsa.Diagnostics.IntegrationTests/ServerLogsAuthorizationTests.cs`.
- [ ] T036 [P] [US2] Add hub authorization and filter-update tests in `test/integration/Elsa.Diagnostics.IntegrationTests/ServerLogsHubAuthorizationTests.cs`.

### Implementation for User Story 2

- [X] T037 [P] [US2] Implement level, category, text, tenant, workflow, trace, correlation, source, and time filter matching in `src/modules/Elsa.Diagnostics/Services/ServerLogFilterEvaluator.cs`.
- [X] T038 [US2] Apply filter evaluation to recent and live provider paths in `src/modules/Elsa.Diagnostics/Providers/InMemory/InMemoryServerLogProvider.cs`.
- [X] T039 [US2] Implement default sensitive-name and text-pattern redaction in `src/modules/Elsa.Diagnostics/Services/ServerLogRedactor.cs`.
- [X] T040 [US2] Apply redaction before publish/buffering in `src/modules/Elsa.Diagnostics/Logging/ServerLogLogger.cs`.
- [X] T041 [US2] Add `read:server-logs` permission constant in `src/modules/Elsa.Diagnostics/Permissions/ServerLogPermissions.cs`.
- [X] T042 [US2] Secure recent endpoint with `read:server-logs` in `src/modules/Elsa.Diagnostics/Endpoints/ServerLogs/Recent/Endpoint.cs`.
- [ ] T043 [US2] Secure SignalR hub with `read:server-logs` in `src/modules/Elsa.Diagnostics/RealTime/ServerLogsHub.cs`.
- [ ] T044 [US2] Add `UpdateFilterAsync` validation and subscription replacement in `src/modules/Elsa.Diagnostics/RealTime/ServerLogsHub.cs`.
- [X] T045 [US2] Clamp recent query size to options in `src/modules/Elsa.Diagnostics/Endpoints/ServerLogs/Recent/Endpoint.cs`.

**Checkpoint**: User Stories 1 and 2 work together with filtering, redaction, and authorization.

---

## Phase 5: User Story 3 - Inspect clustered log sources (Priority: P3)

**Goal**: Operators can view a merged stream with source identity and filter to a specific process/pod/source.

**Independent Test**: Simulate multiple sources through the provider abstraction and verify source listing, merged events, source filtering, and stale-source state.

### Tests for User Story 3

- [ ] T046 [P] [US3] Add source registry tests in `test/unit/Elsa.Diagnostics.UnitTests/Sources/ServerLogSourceRegistryTests.cs`.
- [ ] T047 [P] [US3] Add multi-source provider tests in `test/unit/Elsa.Diagnostics.UnitTests/InMemory/InMemoryServerLogProviderSourceTests.cs`.
- [ ] T048 [P] [US3] Add source-list endpoint integration tests in `test/integration/Elsa.Diagnostics.IntegrationTests/ServerLogsSourcesEndpointTests.cs`.

### Implementation for User Story 3

- [X] T049 [US3] Detect container and Kubernetes environment metadata in `src/modules/Elsa.Diagnostics/Services/ServerLogSourceRegistry.cs`.
- [X] T050 [US3] Track `LastSeen`, stale, disconnected, and unknown health states in `src/modules/Elsa.Diagnostics/Services/ServerLogSourceRegistry.cs`.
- [ ] T051 [US3] Add source-aware merged ordering and deterministic tiebreakers in `src/modules/Elsa.Diagnostics/Providers/InMemory/InMemoryServerLogProvider.cs`.
- [X] T052 [US3] Implement source-list endpoint in `src/modules/Elsa.Diagnostics/Endpoints/ServerLogs/Sources/Endpoint.cs`.
- [X] T053 [US3] Secure source-list endpoint with `read:server-logs` in `src/modules/Elsa.Diagnostics/Endpoints/ServerLogs/Sources/Endpoint.cs`.
- [ ] T054 [US3] Broadcast source changes from the hub in `src/modules/Elsa.Diagnostics/RealTime/ServerLogsHub.cs`.

**Checkpoint**: All stories are independently functional with single-node and simulated clustered source scenarios.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, packaging, validation, and final integration checks.

- [ ] T055 [P] Update package metadata and tags in `src/modules/Elsa.Diagnostics/Elsa.Diagnostics.csproj`.
- [ ] T056 [P] Add setup documentation from quickstart to `src/modules/Elsa.Diagnostics/README.md`.
- [ ] T057 Add diagnostics module reference to any intended sample host in `src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj`.
- [ ] T058 Add sample `UseServerLogStreaming` wiring comment or disabled example in `src/apps/Elsa.Server.Web/Program.cs`.
- [ ] T059 Run quickstart validation and record notes in `specs/003-live-server-logs/quickstart.md`.
- [ ] T060 Run targeted unit tests with `dotnet test test/unit/Elsa.Diagnostics.UnitTests/Elsa.Diagnostics.UnitTests.csproj`.
- [ ] T061 Run targeted integration tests with `dotnet test test/integration/Elsa.Diagnostics.IntegrationTests/Elsa.Diagnostics.IntegrationTests.csproj`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1; blocks all user stories.
- **Phase 3 US1**: Depends on Phase 2; MVP.
- **Phase 4 US2**: Depends on Phase 2 and integrates with US1 provider/hub files.
- **Phase 5 US3**: Depends on Phase 2 and provider/source contracts; can start after provider basics exist.
- **Phase 6 Polish**: Depends on selected user stories.

### User Story Dependencies

- **US1 (P1)**: First executable product slice; no dependency on US2/US3.
- **US2 (P2)**: Builds on US1 capture/provider/hub paths to add security and filters.
- **US3 (P3)**: Builds on source model/provider paths and can be implemented after US1 provider basics.

### Parallel Opportunities

- T003 and T004 can run in parallel after T001.
- T007 through T011 and T013 through T014 can run in parallel once T006 exists.
- US1 tests T019 through T023 can run in parallel.
- US2 tests T033 through T036 can run in parallel.
- US3 tests T046 through T048 can run in parallel.
- Documentation T055 and T056 can run in parallel after implementation stabilizes.

## Parallel Example: User Story 1

```text
Task: "Add ring buffer capacity and ordering tests in test/unit/Elsa.Diagnostics.UnitTests/InMemory/RingBufferTests.cs"
Task: "Add in-memory provider recent/live tests in test/unit/Elsa.Diagnostics.UnitTests/InMemory/InMemoryServerLogProviderTests.cs"
Task: "Add logger provider capture tests in test/unit/Elsa.Diagnostics.UnitTests/Logging/ServerLogLoggerProviderTests.cs"
Task: "Add recent endpoint integration test in test/integration/Elsa.Diagnostics.IntegrationTests/ServerLogsRecentEndpointTests.cs"
Task: "Add SignalR live subscription integration test in test/integration/Elsa.Diagnostics.IntegrationTests/ServerLogsHubTests.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 only.
3. Validate recent backfill and live tailing locally.
4. Stop for review before adding filters, redaction, or cluster source UX.

### Incremental Delivery

1. US1: in-process recent + live stream.
2. US2: production safety through authorization, redaction, and filters.
3. US3: clustered source listing and source-specific views.
4. Polish: docs, sample wiring, and targeted test runs.

## Notes

- Keep the in-memory provider bounded at every queue/buffer boundary.
- Redaction must happen before provider buffering.
- Do not implement Redis, OpenTelemetry, Loki, Seq, Elasticsearch, or Application Insights providers in this feature slice.
- Keep source identity in contracts even when using the in-memory provider.
