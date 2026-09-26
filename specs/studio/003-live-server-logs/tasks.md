# Tasks: Server Logs Studio Module

**Input**: Design documents from `/specs/003-live-server-logs/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification defines independent tests and the plan calls out filter mapping, service behavior, unavailable backend, and SignalR disposal verification.

**Organization**: Tasks are grouped by user story to keep each increment independently testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks in the same phase after prerequisites are satisfied.
- **[Story]**: User story label from [spec.md](./spec.md).
- Every task includes the primary file path to edit or create.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Studio module shell and register it with the solution.

- [X] T001 Create `src/modules/Elsa.Studio.ServerLogs/Elsa.Studio.ServerLogs.csproj` with references to Studio shared/core projects and SignalR client packages already used by Studio.
- [X] T002 Add `src/modules/Elsa.Studio.ServerLogs/Elsa.Studio.ServerLogs.csproj` to `Elsa.Studio.sln`.
- [X] T003 Create `src/modules/Elsa.Studio.ServerLogs/_Imports.razor` with module imports matching adjacent Studio modules.
- [X] T004 Create `src/modules/Elsa.Studio.ServerLogs/Feature.cs` for the Studio feature class.
- [X] T005 Create `src/modules/Elsa.Studio.ServerLogs/Extensions/ServiceCollectionExtensions.cs` with `AddServerLogsModule`.
- [X] T006 Create `src/modules/Elsa.Studio.ServerLogs/Menu/ServerLogsMenu.cs` for the Server Logs menu item.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define shared models, contracts, and backend feature gating required by all stories.

**Critical**: No user story work should begin until this phase is complete.

- [X] T007 Create `src/modules/Elsa.Studio.ServerLogs/Models/ServerLogLevel.cs` for UI log level values.
- [X] T008 [P] Create `src/modules/Elsa.Studio.ServerLogs/Models/ServerLogEvent.cs` from `data-model.md`.
- [X] T009 [P] Create `src/modules/Elsa.Studio.ServerLogs/Models/ServerLogSource.cs` from `data-model.md`.
- [X] T010 [P] Create `src/modules/Elsa.Studio.ServerLogs/Models/ServerLogFilter.cs` from `data-model.md`.
- [X] T011 [P] Create `src/modules/Elsa.Studio.ServerLogs/Models/ServerLogConnectionStatus.cs` for disconnected, connecting, connected, reconnecting, unavailable, and unauthorized states.
- [X] T012 [P] Create `src/modules/Elsa.Studio.ServerLogs/Models/ServerLogViewState.cs` for local row cap, pause, auto-scroll, wrap, compact, and discarded-row state.
- [X] T013 Create `src/modules/Elsa.Studio.ServerLogs/Client/IServerLogsApi.cs` from `contracts/backend-client.md`.
- [X] T014 [P] Create `src/modules/Elsa.Studio.ServerLogs/Contracts/IServerLogService.cs` for recent logs and source loading.
- [X] T015 [P] Create `src/modules/Elsa.Studio.ServerLogs/Contracts/IServerLogObserver.cs` for SignalR live subscription lifecycle.
- [X] T016 Add remote feature gating metadata to `src/modules/Elsa.Studio.ServerLogs/Feature.cs`.
- [X] T017 Register services and menu provider in `src/modules/Elsa.Studio.ServerLogs/Extensions/ServiceCollectionExtensions.cs`.

**Checkpoint**: Module shell, models, contracts, API client, and feature gating compile.

---

## Phase 3: User Story 1 - Watch live backend logs (Priority: P1) MVP

**Goal**: A developer or administrator opens Studio, sees recent backend logs, and receives live updates.

**Independent Test**: Run Studio against an enabled backend or mock service, emit logs, and verify recent and live rows render with auto-scroll and pause/resume controls.

### Tests for User Story 1

- [ ] T018 [P] [US1] Add recent-log service tests in `src/modules/Elsa.Studio.ServerLogs.Tests/RemoteServerLogServiceTests.cs`.
- [ ] T019 [P] [US1] Add SignalR observer lifecycle tests in `src/modules/Elsa.Studio.ServerLogs.Tests/SignalRServerLogObserverTests.cs`.
- [ ] T020 [P] [US1] Add basic page state tests in `src/modules/Elsa.Studio.ServerLogs.Tests/ServerLogsPageTests.cs`.

### Implementation for User Story 1

- [X] T021 [US1] Implement recent-log loading in `src/modules/Elsa.Studio.ServerLogs/Services/RemoteServerLogService.cs`.
- [X] T022 [US1] Implement hub URL construction and authenticated connection setup in `src/modules/Elsa.Studio.ServerLogs/Services/SignalRServerLogObserver.cs`.
- [X] T023 [US1] Implement subscribe, unsubscribe, event callback, connection status, and disposal in `src/modules/Elsa.Studio.ServerLogs/Services/SignalRServerLogObserver.cs`.
- [X] T024 [US1] Create `/server-logs` route markup in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor`.
- [X] T025 [US1] Implement page initialization, recent backfill, live observer startup, and disposal in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor.cs`.
- [X] T026 [US1] Implement compact log row rendering in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor`.
- [X] T027 [US1] Implement pause/resume, clear, reconnect, auto-scroll, wrap, and compact toolbar actions in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor.cs`.
- [X] T028 [US1] Add local row cap and discarded-row indicator in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor.cs`.

**Checkpoint**: User Story 1 is functional and testable as a single-page live log viewer against an enabled backend.

---

## Phase 4: User Story 2 - Find relevant operational events (Priority: P2)

**Goal**: Operators can filter noisy logs by level, text, category, tenant, workflow instance, trace/correlation, and time.

**Independent Test**: Use mocked recent/live events with mixed levels, categories, workflow IDs, tenants, and sources; verify filters update visible rows, REST queries, and the live subscription.

### Tests for User Story 2

- [X] T029 [P] [US2] Add filter-to-query mapping tests in `src/modules/Elsa.Studio.ServerLogs.Tests/ServerLogFilterMapperTests.cs`.
- [ ] T030 [P] [US2] Add observer filter-update tests in `src/modules/Elsa.Studio.ServerLogs.Tests/SignalRServerLogObserverFilterTests.cs`.
- [ ] T031 [P] [US2] Add URL filter state tests in `src/modules/Elsa.Studio.ServerLogs.Tests/ServerLogUrlStateTests.cs`.

### Implementation for User Story 2

- [X] T032 [US2] Implement filter-to-REST query mapping in `src/modules/Elsa.Studio.ServerLogs/Services/ServerLogFilterMapper.cs`.
- [X] T033 [US2] Apply level, text, category, tenant, workflow instance, trace/correlation, and time filters in `src/modules/Elsa.Studio.ServerLogs/Services/RemoteServerLogService.cs`.
- [X] T034 [US2] Implement SignalR filter update in `src/modules/Elsa.Studio.ServerLogs/Services/SignalRServerLogObserver.cs`.
- [X] T035 [US2] Add filter controls to `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor`.
- [X] T036 [US2] Add debounced filter refresh and live subscription update in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor.cs`.
- [X] T037 [US2] Preserve primary filters in URL query string in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor.cs`.
- [X] T038 [US2] Add unauthorized, unavailable, disconnected, reconnecting, empty, and no-match states in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor`.
- [X] T039 [US2] Add copy selected and copy visible behavior in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor.cs`.

**Checkpoint**: User Stories 1 and 2 work together with filterable, shareable operational log views.

---

## Phase 5: User Story 3 - Diagnose clustered deployments by source (Priority: P3)

**Goal**: Operators default to a merged stream and can focus on one backend pod/process/source when needed.

**Independent Test**: Mock a backend with multiple sources, verify `All sources` default, source health display, source-specific filtering, and row source badges.

### Tests for User Story 3

- [ ] T040 [P] [US3] Add source loading tests in `src/modules/Elsa.Studio.ServerLogs.Tests/ServerLogSourceServiceTests.cs`.
- [X] T041 [P] [US3] Add source filter mapping tests in `src/modules/Elsa.Studio.ServerLogs.Tests/ServerLogFilterMapperTests.cs`.
- [ ] T042 [P] [US3] Add page source selector tests in `src/modules/Elsa.Studio.ServerLogs.Tests/ServerLogsSourceSelectorTests.cs`.

### Implementation for User Story 3

- [X] T043 [US3] Implement source loading in `src/modules/Elsa.Studio.ServerLogs/Services/RemoteServerLogService.cs`.
- [X] T044 [US3] Add source selector with `All sources` option in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor`.
- [X] T045 [US3] Apply selected source filter to recent and live subscriptions in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor.cs`.
- [X] T046 [US3] Render source badges and long pod names safely in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor`.
- [X] T047 [US3] Render source health indicators for connected, stale, disconnected, and unknown sources in `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor`.
- [X] T048 [US3] Handle source change notifications from the observer in `src/modules/Elsa.Studio.ServerLogs/Services/SignalRServerLogObserver.cs`.

**Checkpoint**: All stories are independently functional with merged and source-specific log views.

---

## Phase 6: Workflow Context & Polish

**Purpose**: Add contextual navigation, packaging, docs, and final validation.

- [X] T049 Add workflow instance viewer action/link to `/server-logs?workflowInstanceId=...` in `src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/WorkflowInstanceDetails.razor.cs`.
- [X] T050 Add workflow instance query handling to `src/modules/Elsa.Studio.ServerLogs/UI/Pages/ServerLogs.razor.cs`.
- [X] T051 Add module reference to bundled Studio in `src/bundles/Elsa.Studio/Elsa.Studio.csproj`.
- [X] T052 [P] Add setup documentation from quickstart to `src/modules/Elsa.Studio.ServerLogs/README.md`.
- [X] T053 [P] Add package metadata and tags in `src/modules/Elsa.Studio.ServerLogs/Elsa.Studio.ServerLogs.csproj`.
- [ ] T054 Validate the quickstart against a backend with log streaming enabled and update `specs/003-live-server-logs/quickstart.md`.
- [X] T055 Run targeted Studio build with `dotnet build Elsa.Studio.sln`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1; blocks all user stories.
- **Phase 3 US1**: Depends on Phase 2; MVP.
- **Phase 4 US2**: Depends on Phase 2 and integrates with US1 page/service/observer files.
- **Phase 5 US3**: Depends on source models from Phase 2 and can start after service/page basics from US1.
- **Phase 6 Workflow Context & Polish**: Depends on selected user stories.

### User Story Dependencies

- **US1 (P1)**: First executable product slice; no dependency on US2/US3.
- **US2 (P2)**: Builds on US1 service, observer, and page structures to add filters and states.
- **US3 (P3)**: Builds on US1 and source models to add source-aware clustered UX.

### Parallel Opportunities

- T008 through T012 and T014 through T015 can run in parallel after T007.
- US1 tests T018 through T020 can run in parallel.
- US2 tests T029 through T031 can run in parallel.
- US3 tests T040 through T042 can run in parallel.
- Polish docs and metadata T052 through T053 can run in parallel.

## Parallel Example: User Story 2

```text
Task: "Add filter-to-query mapping tests in src/modules/Elsa.Studio.ServerLogs.Tests/ServerLogFilterMapperTests.cs"
Task: "Add observer filter-update tests in src/modules/Elsa.Studio.ServerLogs.Tests/SignalRServerLogObserverFilterTests.cs"
Task: "Add URL filter state tests in src/modules/Elsa.Studio.ServerLogs.Tests/ServerLogUrlStateTests.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 only.
3. Validate recent backfill and live tailing against an enabled backend.
4. Stop for review before adding filter breadth, source UX, or workflow links.

### Incremental Delivery

1. US1: basic Server Logs page with REST backfill and SignalR live stream.
2. US2: filters, URL state, copy actions, and connection/error states.
3. US3: merged/source-specific clustered view.
4. Workflow context and polish.

## Notes

- Keep the page dense and operational; do not build a marketing/dashboard landing page.
- Dispose SignalR connections on navigation and backend environment changes.
- Preserve source filtering as `All sources` equals no `SourceId`.
- Align the final backend remote feature name with the Core implementation before coding T016.
