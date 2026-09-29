# Tasks: Diagnostics Console Logs Studio Module

**Input**: Design documents from `/specs/006-diagnostics-console-logs/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included because the feature specification defines independent tests for each user story and the plan calls for xUnit coverage of filter/query mapping, row/export formatting, and client service behavior.

**Organization**: Tasks are grouped by user story so each increment can be implemented and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks in the same phase because it edits different files and has no dependency on incomplete tasks.
- **[Story]**: Maps to the user story phase: [US1], [US2], [US3].
- Every task includes an exact repository file path.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Studio module/test project shell and wire it into the solution without adding feature behavior yet.

- [X] T001 Create module project file `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Elsa.Studio.Diagnostics.ConsoleLogs.csproj` based on diagnostics module dependencies
- [X] T002 Create test project file `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/Elsa.Studio.Diagnostics.ConsoleLogs.Tests.csproj` with xUnit references and project reference to ConsoleLogs
- [X] T003 Add ConsoleLogs module and test project entries to `Elsa.Studio.sln`
- [X] T004 Add ConsoleLogs module reference to `src/bundles/Elsa.Studio/Elsa.Studio.csproj`
- [X] T005 [P] Create module imports file `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/_Imports.razor`
- [X] T006 [P] Create scoped style placeholder `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor.css` and static script placeholder `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/wwwroot/consoleLogs.js`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared module identity, contracts, models, service registration, and source/menu boundaries required before user story work.

**CRITICAL**: No user story work should begin until this phase is complete.

- [X] T007 Create remote-gated feature class with remote feature constant in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Feature.cs`
- [X] T008 Create service registration extension in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Extensions/ServiceCollectionExtensions.cs`
- [X] T009 Create Diagnostics Console menu entry shell in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Menu/ConsoleLogsMenu.cs`
- [X] T010 [P] Create `ConsoleLogStream` enum in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/ConsoleLogStream.cs`
- [X] T011 [P] Create `ConsoleLogSourceHealth` enum in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/ConsoleLogSourceHealth.cs`
- [X] T012 [P] Create `ConsoleLogConnectionStatus` enum in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/ConsoleLogConnectionStatus.cs`
- [X] T013 [P] Create `ConsoleLogSource` model in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/ConsoleLogSource.cs`
- [X] T014 [P] Create `ConsoleLogLine` model in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/ConsoleLogLine.cs`
- [X] T015 [P] Create `ConsoleLogFilter` model in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/ConsoleLogFilter.cs`
- [X] T016 [P] Create `ConsoleLogViewState` model in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/ConsoleLogViewState.cs`
- [X] T017 [P] Create `ConsoleLogDroppedLineSummary` model in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/ConsoleLogDroppedLineSummary.cs`
- [X] T018 [P] Create `RecentConsoleLinesResult` model in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Models/RecentConsoleLinesResult.cs`
- [X] T019 [P] Create API client contract `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Client/IConsoleLogsApi.cs`
- [X] T020 [P] Create service contract `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Contracts/IConsoleLogService.cs`
- [X] T021 [P] Create observer contract `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Contracts/IConsoleLogObserver.cs`
- [X] T022 Update `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Extensions/ServiceCollectionExtensions.cs` to register the API client, service, observer, and mapper/formatter services

**Checkpoint**: Module shell, shared types, contracts, and DI are ready for user story implementation.

---

## Phase 3: User Story 1 - Watch live console output (Priority: P1) MVP

**Goal**: A developer or administrator opens `/diagnostics/console`, sees recent backend stdout/stderr lines, and receives live updates in a dense terminal-like viewer.

**Independent Test**: Run Studio against a backend that advertises diagnostics console logs, provide recent and live console lines, and verify the page loads backfill, connects to live streaming, and renders stdout and stderr distinctly.

### Tests for User Story 1

- [X] T023 [P] [US1] Add serialization tests for recent console lines and source metadata in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogSerializationTests.cs`
- [X] T024 [P] [US1] Add recent-request mapper tests for default row cap, stdout/stderr stream values, and raw text preservation in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogFilterMapperTests.cs`
- [X] T025 [P] [US1] Add live observer lifecycle tests for connect, subscribe, status, dispose, backend switch, and generic stream error behavior in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/SignalRConsoleLogObserverTests.cs`

### Implementation for User Story 1

- [X] T026 [US1] Implement recent and live filter mapping in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Services/ConsoleLogFilterMapper.cs`
- [X] T027 [US1] Implement recent backfill and source loading service in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Services/RemoteConsoleLogService.cs`
- [X] T028 [US1] Implement authenticated SignalR observer with subscribe, source change, dropped-line, status, and dispose handlers in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Services/SignalRConsoleLogObserver.cs`
- [X] T029 [US1] Create Console page markup with route `/diagnostics/console`, unavailable/unauthorized/loading/connecting states, and dense row list in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor`
- [X] T030 [US1] Implement Console page code-behind for feature gate, recent load before live start, row append, stderr/stdout distinction, generic API/stream error state, backend-change capability reload, and disposal in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor.cs`
- [X] T031 [US1] Add terminal-like dense row styling, stderr styling, long-line safeguards, and truncation/dropped indicators in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor.css`
- [X] T032 [US1] Ensure ConsoleLogs JavaScript interop initializes safely for follow-tail hooks without behavior dependencies in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/wwwroot/consoleLogs.js`

**Checkpoint**: User Story 1 is functional and testable as an MVP.

---

## Phase 4: User Story 2 - Control and preserve the viewing session (Priority: P2)

**Goal**: An operator can pause, resume, follow tail, clear the local view, reconnect, copy/export visible lines, toggle wrap/compact/raw ANSI display, and retain active filters.

**Independent Test**: Use mocked recent and live console lines, then exercise pause/resume, follow-tail, clear local view, reconnect, copy, export, wrap, compact mode, raw ANSI mode, and local row cap behavior.

### Tests for User Story 2

- [X] T033 [P] [US2] Add export/copy formatter tests for timestamp, stream, source, and raw text output in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogExportFormatterTests.cs`
- [X] T034 [P] [US2] Add viewer buffer tests for local row cap, discarded row count, pending paused rows, and clear-local behavior in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogViewStateTests.cs`
- [X] T035 [P] [US2] Add reconnect behavior tests for preserving current filter and status transitions in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/SignalRConsoleLogObserverTests.cs`

### Implementation for User Story 2

- [X] T036 [US2] Implement visible-row copy/export formatter in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Services/ConsoleLogExportFormatter.cs`
- [X] T037 [US2] Add pause/resume, pending-line count, clear local view, row cap pruning, and discarded-row tracking to `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor.cs`
- [X] T038 [US2] Add viewer toolbar controls for pause/resume, follow-tail, clear, reconnect, copy, export, wrap, compact, and raw ANSI display in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor`
- [X] T039 [US2] Add reconnect command handling that preserves active filter and URL state in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor.cs`
- [X] T040 [US2] Add copy/export UI feedback and partial-data indicators for truncated or dropped lines in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor`
- [X] T041 [US2] Add wrap, compact, paused, pending, and raw ANSI display styles in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor.css`
- [X] T042 [US2] Implement scroll/follow-tail interop used by the viewer toolbar in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/wwwroot/consoleLogs.js`

**Checkpoint**: User Stories 1 and 2 work independently against mocked or live console data.

---

## Phase 5: User Story 3 - Filter by source, stream, and text (Priority: P3)

**Goal**: An operator can start in `All sources`, filter by stable `source.id`, choose stdout/stderr/both, use server-side text search with highlights, and share/bookmark URL state.

**Independent Test**: Mock multiple console sources with different health states, streams, and line text; verify source, stream, text, and time filters affect recent loading, live streaming, rendered rows, and URL query state.

### Tests for User Story 3

- [X] T043 [P] [US3] Add URL-state mapper tests for `source`, `stream`, `text`, `from`, `to`, `wrap`, `compact`, `ansi`, and `follow` in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogUrlStateMapperTests.cs`
- [X] T044 [P] [US3] Add source selector tests for stable `source.id`, stale source preservation, and display metadata labels in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogSourceSelectorTests.cs`
- [X] T045 [P] [US3] Add text-highlight tests that preserve raw text and highlight returned visible matches in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogTextHighlighterTests.cs`

### Implementation for User Story 3

- [X] T046 [US3] Implement URL query parsing and writing for canonical Console parameters in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Services/ConsoleLogUrlStateMapper.cs`
- [X] T047 [US3] Extend `ConsoleLogFilterMapper` to send stable source, stream, server-side text, and UTC time filters to recent and live contracts in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Services/ConsoleLogFilterMapper.cs`
- [X] T048 [US3] Implement text highlight helper that preserves underlying raw line text in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/Services/ConsoleLogTextHighlighter.cs`
- [X] T049 [US3] Add source selector, stream selector, text input, and UTC time filter controls to `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor`
- [X] T050 [US3] Wire filter changes to recent reload, live subscription update, URL state updates, empty/no-match states, and validation feedback in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor.cs`
- [X] T051 [US3] Render source labels with `displayName` plus service/process/machine/pod/container/namespace/node metadata in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor`
- [X] T052 [US3] Style source health, stale/disconnected source states, selected source, and highlighted text matches in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/UI/Pages/ConsoleLogs.razor.css`

**Checkpoint**: All user stories are independently functional and preserve shareable Console URL state.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verify the finished module, documentation, and coexistence with Structured Logs.

- [X] T053 [P] Update module README with route, feature gate, permission, contracts, and Structured Logs distinction in `src/modules/Elsa.Studio.Diagnostics.ConsoleLogs/README.md`
- [X] T054 [P] Update feature quickstart findings after implementation in `specs/006-diagnostics-console-logs/quickstart.md`
- [X] T055 Verify Structured Logs route and menu coexistence remains unchanged in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Menu/StructuredLogsMenu.cs`
- [X] T056 Run ConsoleLogs tests with `dotnet test src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/Elsa.Studio.Diagnostics.ConsoleLogs.Tests.csproj` and record result in `specs/006-diagnostics-console-logs/quickstart.md`
- [X] T057 Run solution build with `dotnet build Elsa.Studio.sln` and record result in `specs/006-diagnostics-console-logs/quickstart.md`
- [ ] T058 Manually verify unavailable, unauthorized, generic API/stream error, backend-switch, disconnected, reconnecting, empty, no-match, stale-source, long-line, ANSI, dropped-line, and 10,000-row states using `specs/006-diagnostics-console-logs/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup** has no dependencies.
- **Phase 2: Foundational** depends on Phase 1 and blocks all user stories.
- **Phase 3: User Story 1** depends on Phase 2 and is the MVP.
- **Phase 4: User Story 2** depends on Phase 2 and can proceed after the shared page shell exists; validate with US1 for end-to-end behavior.
- **Phase 5: User Story 3** depends on Phase 2 and can proceed after filter contracts exist; validate with US1 live/recent flows.
- **Phase 6: Polish** depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories after foundation; delivers initial recent/live viewer.
- **US2 (P2)**: Uses the viewer shell and live session from US1 for full end-to-end verification, but controls/formatter/buffer tests are independently testable.
- **US3 (P3)**: Uses the service and observer contracts from foundation; source/filter URL mapping is independently testable before UI integration.

### Parallel Opportunities

- T005-T006 can run in parallel after T001.
- T010-T021 can run in parallel after project files exist.
- T023-T025 can run in parallel before US1 implementation.
- T033-T035 can run in parallel before US2 implementation.
- T043-T045 can run in parallel before US3 implementation.
- T053-T054 can run in parallel during final documentation polish.

---

## Parallel Example: User Story 1

```text
Task: "T023 [P] [US1] Add serialization tests for recent console lines and source metadata in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogSerializationTests.cs"
Task: "T024 [P] [US1] Add recent-request mapper tests for default row cap, stdout/stderr stream values, and raw text preservation in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogFilterMapperTests.cs"
Task: "T025 [P] [US1] Add live observer lifecycle tests for connect, subscribe, status, dispose, backend switch, and generic stream error behavior in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/SignalRConsoleLogObserverTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "T033 [P] [US2] Add export/copy formatter tests for timestamp, stream, source, and raw text output in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogExportFormatterTests.cs"
Task: "T034 [P] [US2] Add viewer buffer tests for local row cap, discarded row count, pending paused rows, and clear-local behavior in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogViewStateTests.cs"
Task: "T035 [P] [US2] Add reconnect behavior tests for preserving current filter and status transitions in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/SignalRConsoleLogObserverTests.cs"
```

## Parallel Example: User Story 3

```text
Task: "T043 [P] [US3] Add URL-state mapper tests for source, stream, text, from, to, wrap, compact, ansi, and follow in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogUrlStateMapperTests.cs"
Task: "T044 [P] [US3] Add source selector tests for stable source.id, stale source preservation, and display metadata labels in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogSourceSelectorTests.cs"
Task: "T045 [P] [US3] Add text-highlight tests that preserve raw text and highlight returned visible matches in src/modules/Elsa.Studio.Diagnostics.ConsoleLogs.Tests/ConsoleLogTextHighlighterTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundation.
3. Complete Phase 3 User Story 1.
4. Validate `/diagnostics/console` loads recent lines, connects live, distinguishes stdout/stderr, and disposes the live stream cleanly.

### Incremental Delivery

1. Add US1 for a working recent/live raw console viewer.
2. Add US2 for operational controls, local buffering, copy/export, and viewer settings.
3. Add US3 for source/stream/text/time filtering and shareable URL state.
4. Finish with Polish verification and documentation.

### Team Parallel Strategy

After Phase 2, one developer can finish US1 viewer/live behavior while another builds US2 formatter/buffer controls and another builds US3 URL/filter mapper tests. UI integration should be coordinated because `ConsoleLogs.razor` and `ConsoleLogs.razor.cs` are shared files.

## Notes

- Keep all work scoped to `elsa-studio`; do not modify `elsa-core`.
- Do not parse console lines into structured log records or add semantic log fields such as level, category, template, properties, scopes, trace, or span.
- Use `source.id` for filters and subscriptions; use `displayName` and metadata only for labels.
- Preserve raw text for copy/export even when raw ANSI display or highlighting is enabled.
