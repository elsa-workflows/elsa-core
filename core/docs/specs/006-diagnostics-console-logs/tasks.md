# Tasks: Diagnostics Console Logs

**Input**: Design documents from `/specs/006-diagnostics-console-logs/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification requires validation coverage for capture, stream identity, filtering, buffering, dropped counts, authorization, redaction, source health, provider boundaries, and feedback-loop prevention.

**Organization**: Tasks are grouped by user story so each increment can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks after prerequisites are satisfied.
- **[Story]**: User story label from [spec.md](./spec.md).
- Every task includes the primary file path to edit or create.

## Phase 1: Setup

**Purpose**: Create the Core module and test project skeletons.

- [X] T001 [P] Create console logs module project in `src/modules/Elsa.Diagnostics.ConsoleLogs/Elsa.Diagnostics.ConsoleLogs.csproj`.
- [X] T002 [P] Create unit test project in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Elsa.Diagnostics.ConsoleLogs.UnitTests.csproj`.
- [X] T003 [P] Create integration test project in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/Elsa.Diagnostics.ConsoleLogs.IntegrationTests.csproj`.
- [X] T004 Add the console logs projects to `Elsa.sln`.
- [X] T005 [P] Add module global usings in `src/modules/Elsa.Diagnostics.ConsoleLogs/Usings.cs`.
- [X] T006 [P] Add unit test global usings in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Usings.cs`.
- [X] T007 [P] Add integration test global usings in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/Usings.cs`.

---

## Phase 2: Foundational

**Purpose**: Add shared contracts, models, options, registration, and permissions that block all user stories.

**Critical**: No user story work should begin until these shared contracts and registration surfaces exist.

- [X] T008 [P] Add console log models in `src/modules/Elsa.Diagnostics.ConsoleLogs/Models/ConsoleLogLine.cs`.
- [X] T009 [P] Add console source model and health enum in `src/modules/Elsa.Diagnostics.ConsoleLogs/Models/ConsoleLogSource.cs`.
- [X] T010 [P] Add filter, recent result, dropped summary, and stream item models in `src/modules/Elsa.Diagnostics.ConsoleLogs/Models/ConsoleLogFilter.cs`.
- [X] T011 [P] Add provider contract in `src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogProvider.cs`.
- [X] T012 [P] Add redactor contract in `src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogRedactor.cs`.
- [X] T013 [P] Add source registry contract in `src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogSourceRegistry.cs`.
- [X] T014 [P] Add capture contract in `src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogCapture.cs`.
- [X] T015 [P] Add host options in `src/modules/Elsa.Diagnostics.ConsoleLogs/Options/ConsoleLogsOptions.cs`.
- [X] T016 [P] Add permission constant in `src/modules/Elsa.Diagnostics.ConsoleLogs/Permissions/ConsoleLogsPermissions.cs`.
- [X] T017 Add service registration extension in `src/modules/Elsa.Diagnostics.ConsoleLogs/Extensions/ServiceCollectionExtensions.cs`.
- [X] T018 Add application and endpoint route extensions in `src/modules/Elsa.Diagnostics.ConsoleLogs/Extensions/ApplicationBuilderExtensions.cs`.
- [X] T019 Add module feature registration in `src/modules/Elsa.Diagnostics.ConsoleLogs/Features/ConsoleLogsFeature.cs`.
- [X] T020 Add shell feature registration in `src/modules/Elsa.Diagnostics.ConsoleLogs/ShellFeatures/ConsoleLogsFeature.cs`.
- [X] T021 [P] Add feature and naming contract tests in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/ConsoleLogsNamingTests.cs`.
- [X] T022 [P] Add default registration tests in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/ConsoleLogsRegistrationTests.cs`.

**Checkpoint**: Shared contracts, options, permission, and feature registration are ready for story work.

---

## Phase 3: User Story 1 - Tail raw backend console output (Priority: P1) MVP

**Goal**: Authorized users can request recent stdout/stderr lines and receive live console line events while host console output still reaches its original destination.

**Independent Test**: Enable console logs, write distinct complete stdout/stderr lines, request recent lines, subscribe live, and verify ordered source-aware events plus preserved original console output.

### Tests for User Story 1

- [X] T023 [P] [US1] Add capture tee preservation tests in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Capture/ConsoleCaptureTeeTests.cs`.
- [X] T024 [P] [US1] Add partial-line buffering and idle flush tests in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Capture/ConsoleLineBufferTests.cs`.
- [X] T025 [P] [US1] Add truncation and ANSI default handling tests in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Capture/ConsoleLineFormatterTests.cs`.
- [X] T026 [P] [US1] Add in-memory recent and live provider tests in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/InMemory/InMemoryConsoleLogProviderTests.cs`.
- [X] T027 [P] [US1] Add recent endpoint contract tests in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsRecentEndpointTests.cs`.
- [X] T028 [P] [US1] Add SignalR subscribe/unsubscribe integration tests in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsHubTests.cs`.

### Implementation for User Story 1

- [X] T029 [P] [US1] Implement line buffer in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleLineBuffer.cs`.
- [X] T030 [P] [US1] Implement line formatter for truncation and ANSI handling in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleLineFormatter.cs`.
- [X] T031 [US1] Implement stdout/stderr capture tee in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleCaptureTee.cs`.
- [X] T032 [US1] Implement in-memory provider with bounded recent history and live queues in `src/modules/Elsa.Diagnostics.ConsoleLogs/Providers/InMemory/InMemoryConsoleLogProvider.cs`.
- [X] T033 [US1] Implement recent endpoint in `src/modules/Elsa.Diagnostics.ConsoleLogs/Endpoints/ConsoleLogs/Recent/Endpoint.cs`.
- [X] T034 [US1] Implement SignalR client contract in `src/modules/Elsa.Diagnostics.ConsoleLogs/RealTime/IConsoleLogsClient.cs`.
- [X] T035 [US1] Implement SignalR hub subscribe and unsubscribe flow in `src/modules/Elsa.Diagnostics.ConsoleLogs/RealTime/ConsoleLogsHub.cs`.
- [X] T036 [US1] Wire capture startup and shutdown in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleLogCaptureHostedService.cs`.
- [X] T037 [US1] Run `dotnet test test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Elsa.Diagnostics.ConsoleLogs.UnitTests.csproj`.

**Checkpoint**: User Story 1 is fully functional and independently testable as the MVP.

---

## Phase 4: User Story 2 - Filter, secure, and redact console output (Priority: P2)

**Goal**: Operators can filter console output while Core enforces a dedicated permission and redacts sensitive line text and source metadata before provider boundaries.

**Independent Test**: Connect authorized and unauthorized callers, write secret-like console lines, apply filters, and verify unauthorized access is rejected while authorized callers receive only redacted matching lines.

### Tests for User Story 2

- [X] T038 [P] [US2] Add redaction tests for line text and source metadata in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Redaction/ConsoleLogRedactorTests.cs`.
- [X] T039 [P] [US2] Add filter evaluator tests for source, stream, query, time range, and limit in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Filtering/ConsoleLogFilterTests.cs`.
- [X] T040 [P] [US2] Add authorization tests for recent and source endpoints in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsAuthorizationTests.cs`.
- [X] T041 [P] [US2] Add SignalR authorization and filter-update tests in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsHubAuthorizationTests.cs`.
- [X] T042 [P] [US2] Add redaction-before-provider boundary tests in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Redaction/ConsoleLogProviderRedactionTests.cs`.

### Implementation for User Story 2

- [X] T043 [P] [US2] Implement default redaction rules in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleLogRedactor.cs`.
- [X] T044 [P] [US2] Implement filter evaluator in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleLogFilterEvaluator.cs`.
- [X] T045 [US2] Apply redaction before provider publication in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleCaptureTee.cs`.
- [X] T046 [US2] Enforce server-clamped recent query limits in `src/modules/Elsa.Diagnostics.ConsoleLogs/Endpoints/ConsoleLogs/Recent/Endpoint.cs`.
- [X] T047 [US2] Secure recent and source endpoints with `read:diagnostics:console-logs` in `src/modules/Elsa.Diagnostics.ConsoleLogs/Endpoints/ConsoleLogs/Recent/Endpoint.cs`.
- [X] T048 [US2] Secure SignalR hub with `read:diagnostics:console-logs` in `src/modules/Elsa.Diagnostics.ConsoleLogs/RealTime/ConsoleLogsHub.cs`.
- [X] T049 [US2] Implement hub filter update behavior in `src/modules/Elsa.Diagnostics.ConsoleLogs/RealTime/ConsoleLogsHub.cs`.
- [X] T050 [US2] Run `dotnet test test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/Elsa.Diagnostics.ConsoleLogs.IntegrationTests.csproj`.

**Checkpoint**: User Stories 1 and 2 work together with authorization, filtering, and redaction.

---

## Phase 5: User Story 3 - Identify console sources in clustered deployments (Priority: P3)

**Goal**: Operators can view source-aware merged console output, filter to one source, and see source health without changing Studio-facing contracts.

**Independent Test**: Simulate multiple provider sources, request sources, subscribe to merged output, filter to one source, and verify source health plus dropped-line metadata.

### Tests for User Story 3

- [X] T051 [P] [US3] Add source registry tests for current source metadata and health transitions in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Sources/ConsoleLogSourceRegistryTests.cs`.
- [X] T052 [P] [US3] Add multi-source ordering tests in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/InMemory/InMemoryConsoleLogProviderSourceTests.cs`.
- [X] T053 [P] [US3] Add dropped-line summary tests for buffer and subscriber overflow in `test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/InMemory/InMemoryConsoleLogProviderDroppedLineTests.cs`.
- [X] T054 [P] [US3] Add source endpoint integration tests in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsSourcesEndpointTests.cs`.
- [X] T055 [P] [US3] Add source status SignalR integration tests in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsHubSourceStatusTests.cs`.

### Implementation for User Story 3

- [X] T056 [P] [US3] Implement source registry in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleLogSourceRegistry.cs`.
- [X] T057 [US3] Add source health timeout handling in `src/modules/Elsa.Diagnostics.ConsoleLogs/Services/ConsoleLogSourceHealthService.cs`.
- [X] T058 [US3] Add deterministic multi-source ordering in `src/modules/Elsa.Diagnostics.ConsoleLogs/Providers/InMemory/InMemoryConsoleLogProvider.cs`.
- [X] T059 [US3] Add dropped-line summary publication in `src/modules/Elsa.Diagnostics.ConsoleLogs/Providers/InMemory/InMemoryConsoleLogProvider.cs`.
- [X] T060 [US3] Implement sources endpoint in `src/modules/Elsa.Diagnostics.ConsoleLogs/Endpoints/ConsoleLogs/Sources/Endpoint.cs`.
- [X] T061 [US3] Stream source status changes through SignalR in `src/modules/Elsa.Diagnostics.ConsoleLogs/RealTime/ConsoleLogsHub.cs`.
- [X] T062 [US3] Run `dotnet test test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Elsa.Diagnostics.ConsoleLogs.UnitTests.csproj`.

**Checkpoint**: All user stories are independently functional and source-aware.

---

## Phase 6: Documentation & Polish

**Purpose**: Update public docs, sample guidance, boundary assertions, and validation.

- [X] T063 [P] Add console logs README in `src/modules/Elsa.Diagnostics.ConsoleLogs/README.md`.
- [X] T064 [P] Update quickstart implementation notes in `specs/006-diagnostics-console-logs/quickstart.md`.
- [X] T065 [P] Add module boundary assertions in `test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsModuleTests.cs`.
- [X] T066 Update sample host wiring only if the sample opts into console logs in `src/apps/Elsa.Server.Web/Program.cs`.
- [X] T067 Run `dotnet build src/modules/Elsa.Diagnostics.ConsoleLogs/Elsa.Diagnostics.ConsoleLogs.csproj`.
- [X] T068 Run `dotnet test test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Elsa.Diagnostics.ConsoleLogs.UnitTests.csproj`.
- [X] T069 Run `dotnet test test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/Elsa.Diagnostics.ConsoleLogs.IntegrationTests.csproj`.
- [X] T070 Run `rg "StructuredLogs|OpenTelemetry|Kubernetes|Docker|OTLP|Loki|Seq" src/modules/Elsa.Diagnostics.ConsoleLogs specs/006-diagnostics-console-logs` and record boundary findings in `specs/006-diagnostics-console-logs/tasks.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user stories.
- **Phase 3 US1**: Depends on Phase 2; MVP.
- **Phase 4 US2**: Depends on Phase 2 and should follow US1 validation for an end-to-end capture path.
- **Phase 5 US3**: Depends on Phase 2 and can proceed after provider/source contracts exist, but final validation should follow US1.
- **Phase 6 Polish**: Depends on selected story implementation.

### User Story Dependencies

- **US1 (P1)**: First executable slice; no dependency on US2 or US3.
- **US2 (P2)**: Uses US1 capture/provider surfaces but remains independently testable through redaction, filtering, authorization, and hub filter updates.
- **US3 (P3)**: Uses shared provider/source contracts and adds multi-source behavior without changing US1 or US2 contracts.

### Parallel Opportunities

- T001 through T003 and T005 through T007 can run in parallel.
- T008 through T016 and T021 through T022 can run in parallel after project creation.
- US1 tests T023 through T028 can run in parallel.
- US2 tests T038 through T042 can run in parallel.
- US3 tests T051 through T055 can run in parallel.
- Documentation tasks T063 through T065 can run in parallel after implementation APIs settle.

## Parallel Example: User Story 1

```text
Task: "Add capture tee preservation tests in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Capture/ConsoleCaptureTeeTests.cs"
Task: "Add partial-line buffering and idle flush tests in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Capture/ConsoleLineBufferTests.cs"
Task: "Add truncation and ANSI default handling tests in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Capture/ConsoleLineFormatterTests.cs"
Task: "Add in-memory recent and live provider tests in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/InMemory/InMemoryConsoleLogProviderTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add redaction tests for line text and source metadata in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Redaction/ConsoleLogRedactorTests.cs"
Task: "Add filter evaluator tests for source, stream, query, time range, and limit in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Filtering/ConsoleLogFilterTests.cs"
Task: "Add authorization tests for recent and source endpoints in test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsAuthorizationTests.cs"
Task: "Add SignalR authorization and filter-update tests in test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsHubAuthorizationTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "Add source registry tests for current source metadata and health transitions in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Sources/ConsoleLogSourceRegistryTests.cs"
Task: "Add multi-source ordering tests in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/InMemory/InMemoryConsoleLogProviderSourceTests.cs"
Task: "Add dropped-line summary tests for buffer and subscriber overflow in test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/InMemory/InMemoryConsoleLogProviderDroppedLineTests.cs"
Task: "Add source endpoint integration tests in test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/ConsoleLogsSourcesEndpointTests.cs"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 only.
3. Run the US1 unit and integration tests.
4. Stop and validate recent backfill, live stdout/stderr streaming, and preserved console output.

### Incremental Delivery

1. US1: raw stdout/stderr capture, bounded recent history, and live streaming.
2. US2: authorization, filtering, redaction, and provider-boundary safety.
3. US3: source health, multi-source ordering, dropped summaries, and source endpoint behavior.
4. Polish: docs, sample guidance, boundary checks, builds, and tests.

## Notes

- Boundary scan result: only explicit out-of-scope documentation references were found; source matches for `Sequence` and `StripAnsiEscapeSequences` are expected identifier matches, not external provider integrations.
- Do not touch `elsa-studio` for this Core feature.
- Do not implement durable console log persistence in this feature.
- Do not implement Kubernetes, Docker, vendor sink, or OpenTelemetry integrations.
- Preserve redaction-before-provider boundaries.
- Keep `Elsa.Diagnostics.StructuredLogs` separate from console logs.
