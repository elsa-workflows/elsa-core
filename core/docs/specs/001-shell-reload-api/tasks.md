# Tasks: Shell Reload API Endpoints

**Input**: Design documents from `/specs/001-shell-reload-api/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Tests are required for this feature by the repository constitution, so each user story includes component-test coverage.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the minimum project references and client resource scaffolding required by the feature.

- [X] T001 Update `src/modules/Elsa.Workflows.Api/Elsa.Workflows.Api.csproj` to reference the `CShells` package needed for `IShellManager`, `IShellSettingsProvider`, and `IShellSettingsCache`
- [X] T002 [P] Create the shell API client contract scaffold in `src/clients/Elsa.Api.Client/Resources/Shells/Contracts/IShellsApi.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared orchestration infrastructure and shared non-endpoint result types used by all shell reload stories.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create shared orchestration result contracts in `src/modules/Elsa.Workflows.Api/Contracts/ShellReloadStatus.cs`, `src/modules/Elsa.Workflows.Api/Contracts/ShellReloadItemResult.cs`, and `src/modules/Elsa.Workflows.Api/Contracts/ShellReloadResult.cs`
- [X] T004 [P] Create shared client-side reload DTOs in `src/clients/Elsa.Api.Client/Resources/Shells/Models/ShellReloadItemResult.cs`, `src/clients/Elsa.Api.Client/Resources/Shells/Models/ShellReloadStatus.cs`, and `src/clients/Elsa.Api.Client/Resources/Shells/Responses/ShellReloadResponse.cs`
- [X] T005 Create the reload orchestration contract and base implementation in `src/modules/Elsa.Workflows.Api/Contracts/IShellReloadOrchestrator.cs` and `src/modules/Elsa.Workflows.Api/Services/ShellReloadOrchestrator.cs`
- [X] T006 Register the shell reload orchestrator in `src/modules/Elsa.Workflows.Api/ShellFeatures/WorkflowsApiFeature.cs`

**Checkpoint**: Shared DTOs and orchestration infrastructure are ready for story implementation.

---

## Phase 3: User Story 1 - Reload all shells after configuration changes (Priority: P1) 🎯 MVP

**Goal**: Let operators trigger a full shell reload and receive detailed per-shell results.

**Independent Test**: Change shell configuration for one or more shells, call the reload-all endpoint, and verify the updated behavior is active without restarting the host.

### Tests for User Story 1

- [X] T007 [P] [US1] Add component tests for successful full-shell reload results in `test/component/Elsa.Workflows.ComponentTests/Scenarios/RestApis/Endpoints/Shells/ReloadAllTests.cs`

### Implementation for User Story 1

- [X] T008 [US1] Implement full-reload snapshotting and successful per-shell result mapping around the current full-reload fallback in `src/modules/Elsa.Workflows.Api/Services/ShellReloadOrchestrator.cs`
- [X] T009 [P] [US1] Implement the reload-all endpoint and collocated response handling in `src/modules/Elsa.Workflows.Api/Endpoints/Shells/ReloadAll/Endpoint.cs` and `src/modules/Elsa.Workflows.Api/Endpoints/Shells/ReloadAll/Models.cs`
- [X] T010 [P] [US1] Add the reload-all client method to `src/clients/Elsa.Api.Client/Resources/Shells/Contracts/IShellsApi.cs`

**Checkpoint**: User Story 1 is independently functional through the API and client surface.

---

## Phase 4: User Story 2 - Request reload for one shell (Priority: P2)

**Goal**: Expose a targeted shell reload endpoint that validates the shell ID and enforces requested-shell strict success semantics while still using the current full-reload fallback.

**Independent Test**: Call the targeted reload endpoint for a known shell and verify the requested shell refreshes; call it for an unknown shell and verify a not-found outcome.

### Tests for User Story 2

- [X] T011 [P] [US2] Add component tests for targeted reload success, unknown shell handling, and requested-shell strict outcomes in `test/component/Elsa.Workflows.ComponentTests/Scenarios/RestApis/Endpoints/Shells/ReloadTests.cs`

### Implementation for User Story 2

- [X] T012 [US2] Extend targeted reload validation and requested-shell strict result handling in `src/modules/Elsa.Workflows.Api/Services/ShellReloadOrchestrator.cs`
- [X] T013 [P] [US2] Implement the targeted reload endpoint and collocated models in `src/modules/Elsa.Workflows.Api/Endpoints/Shells/Reload/Endpoint.cs` and `src/modules/Elsa.Workflows.Api/Endpoints/Shells/Reload/Models.cs`
- [X] T014 [P] [US2] Add the targeted reload client method to `src/clients/Elsa.Api.Client/Resources/Shells/Contracts/IShellsApi.cs`

**Checkpoint**: User Story 2 works independently for known and unknown shell IDs.

---

## Phase 5: User Story 3 - Handle failed reload attempts safely (Priority: P3)

**Goal**: Return clear busy, unavailable, and partial-success outcomes without falsely reporting that shells refreshed.

**Independent Test**: Simulate concurrent reloads, unavailable shell settings, and invalid configuration for one shell, then verify the API returns the required error or partial-success responses with shell-level detail.

### Tests for User Story 3

- [X] T015 [P] [US3] Add component tests for busy rejection, unavailable configuration, and partial-success reload behavior in `test/component/Elsa.Workflows.ComponentTests/Scenarios/RestApis/Endpoints/Shells/ShellReloadFailureTests.cs`

### Implementation for User Story 3

- [X] T016 [US3] Implement busy-state rejection and provider-unavailable failure handling in `src/modules/Elsa.Workflows.Api/Services/ShellReloadOrchestrator.cs`
- [X] T017 [US3] Implement partial-success reconciliation for invalid shell configurations in `src/modules/Elsa.Workflows.Api/Services/ShellReloadOrchestrator.cs`
- [X] T018 [US3] Map busy, partial-success, requested-shell failure, and provider-failure outcomes to HTTP responses in `src/modules/Elsa.Workflows.Api/Endpoints/Shells/ReloadAll/Endpoint.cs` and `src/modules/Elsa.Workflows.Api/Endpoints/Shells/Reload/Endpoint.cs`

**Checkpoint**: All failure and partial-success behaviors are independently testable through the API.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Bring implementation, client surface, and planning artifacts into final alignment.

- [X] T019 [P] Update `specs/001-shell-reload-api/contracts/shell-reload-api.yaml` to match the final implemented routes, statuses, and response payloads
- [X] T020 Run the quickstart validation against `specs/001-shell-reload-api/quickstart.md` using `test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj` and fix any mismatches in `src/modules/Elsa.Workflows.Api/Endpoints/Shells/` and `src/clients/Elsa.Api.Client/Resources/Shells/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1; blocks all user story work.
- **User Story 1 (Phase 3)**: Depends on Phase 2 only.
- **User Story 2 (Phase 4)**: Depends on Phase 2 only.
- **User Story 3 (Phase 5)**: Depends on Phase 2 only.
- **Polish (Phase 6)**: Depends on the user stories you intend to ship.

### User Story Dependencies

- **US1**: No dependency on other user stories; this is the MVP.
- **US2**: No functional dependency on US1, but it reuses the shared orchestrator and response models from Phase 2.
- **US3**: No functional dependency on US1 or US2, but it extends the same orchestrator and endpoint files, so coordinate file ownership if multiple developers work in parallel.

### Within Each User Story

- Write the component tests first and confirm they fail before implementation.
- Implement orchestration logic before endpoint wiring.
- Add API client methods after the corresponding endpoint contract is stable.
- Validate the story through the hosted component-test server before moving on.

### Parallel Opportunities

- `T002` can run in parallel with `T001`.
- `T004` can run in parallel with `T003` once Phase 1 is complete.
- `T007`, `T009`, and `T010` can proceed in parallel after `T008` starts stabilizing the shared orchestration shape.
- `T011`, `T013`, and `T014` can proceed in parallel after `T012` defines the targeted behavior.
- `T015` can run in parallel with `T016` because tests are in a separate file.
- `T019` can run in parallel with final cleanup before `T020`.

---

## Parallel Example: User Story 1

```bash
# After T008 defines the US1 orchestration behavior:
Task: "Add component tests for successful full-shell reload results in test/component/Elsa.Workflows.ComponentTests/Scenarios/RestApis/Endpoints/Shells/ReloadAllTests.cs"
Task: "Implement the reload-all endpoint and collocated response handling in src/modules/Elsa.Workflows.Api/Endpoints/Shells/ReloadAll/Endpoint.cs and src/modules/Elsa.Workflows.Api/Endpoints/Shells/ReloadAll/Models.cs"
Task: "Add the reload-all client method to src/clients/Elsa.Api.Client/Resources/Shells/Contracts/IShellsApi.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Validate the reload-all endpoint and client flow with the component tests.

### Incremental Delivery

1. Deliver US1 to provide immediate operational value.
2. Add US2 so automation can target one shell explicitly while still using the full-reload fallback.
3. Add US3 to complete the failure-handling and partial-success contract.
4. Finish with Phase 6 to ensure the generated contract and quickstart remain accurate.

### Parallel Team Strategy

1. One developer completes Phase 1 and Phase 2.
2. Then split work by story, with coordination around `src/modules/Elsa.Workflows.Api/Services/ShellReloadOrchestrator.cs` and `src/clients/Elsa.Api.Client/Resources/Shells/Contracts/IShellsApi.cs` because those files are shared touchpoints.
3. Rejoin for Phase 6 validation and contract alignment.

---

## Notes

- `[P]` tasks are limited to work that can proceed without editing the same files concurrently.
- Story labels map directly to the prioritized stories in `spec.md`.
- The task list assumes the design decision to keep this feature inside `Elsa.Workflows.Api`, `Elsa.Api.Client`, and `Elsa.Workflows.ComponentTests`.
- `T020` is the final verification checkpoint before handing the feature off for implementation completion.