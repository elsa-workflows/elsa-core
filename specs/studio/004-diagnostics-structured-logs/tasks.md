# Tasks: Diagnostics Structured Logs Studio Module

**Input**: Design documents from `/specs/004-diagnostics-structured-logs/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification defines independent tests and the plan calls out filter/model mapping, structured fields, URL state, unavailable backend, and build verification.

**Organization**: Tasks are grouped by user story so the refactor can be validated incrementally.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks in the same phase after prerequisites are satisfied.
- **[Story]**: User story label from [spec.md](./spec.md).
- Every task includes the primary file path to edit, rename, or create.

## Phase 1: Setup (Shared Refactor Infrastructure)

**Purpose**: Move active module identity from Server Logs to Diagnostics Structured Logs.

- [X] T001 Rename `src/modules/Elsa.Studio.ServerLogs/` to `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/`.
- [X] T002 Rename `src/modules/Elsa.Studio.ServerLogs.Tests/` to `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/`.
- [X] T003 Rename `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Elsa.Studio.ServerLogs.csproj` to `Elsa.Studio.Diagnostics.StructuredLogs.csproj` and update description/package tags.
- [X] T004 Rename `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/Elsa.Studio.ServerLogs.Tests.csproj` to `Elsa.Studio.Diagnostics.StructuredLogs.Tests.csproj`.
- [X] T005 Update `Elsa.Studio.sln` project names and paths for the renamed module and test project.
- [X] T006 Update `src/bundles/Elsa.Studio/Elsa.Studio.csproj` to reference `Elsa.Studio.Diagnostics.StructuredLogs.csproj`.
- [X] T007 Update host imports and registrations in `src/hosts/Elsa.Studio.Host.Server/Program.cs` and `src/hosts/Elsa.Studio.Host.Wasm/Program.cs`.
- [X] T008 Rename static assets in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/wwwroot/` from `serverLogs.*` to `structuredLogs.*` and update host asset links.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Rename public contracts and backend integration before UI story work.

**Critical**: No user story work should begin until this phase is complete.

- [X] T009 Rename root namespace and all imports from `Elsa.Studio.ServerLogs` to `Elsa.Studio.Diagnostics.StructuredLogs` across `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/`.
- [X] T010 Rename test namespace/imports to `Elsa.Studio.Diagnostics.StructuredLogs.Tests` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/StructuredLogFilterMapperTests.cs`.
- [X] T011 Rename public `ServerLog*` model types to `StructuredLog*` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Models/`.
- [X] T012 Rename `RecentServerLogsResult` to `RecentStructuredLogsResult` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Models/`.
- [X] T013 Rename `IServerLogsApi` to `IStructuredLogsApi` and update REST paths to `/diagnostics/structured-logs/*` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Client/IStructuredLogsApi.cs`.
- [X] T014 Rename service contracts to `IStructuredLogService` and `IStructuredLogObserver` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Contracts/`.
- [X] T015 Rename services to `RemoteStructuredLogService`, `StructuredLogFilterMapper`, and `SignalRStructuredLogObserver` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Services/`.
- [X] T016 Update SignalR hub URL to derive `/elsa/hubs/diagnostics/structured-logs` from the configured backend API URL in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Services/SignalRStructuredLogObserver.cs`.
- [X] T017 Update `Feature.RemoteFeatureName` to `Elsa.Diagnostics.StructuredLogs` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Feature.cs`.
- [X] T018 Rename `AddServerLogsModule` to `AddStructuredLogsModule` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Extensions/ServiceCollectionExtensions.cs`.
- [X] T019 Add `MenuItemGroups.Diagnostics` in `src/framework/Elsa.Studio.Core/MenuItemGroups.cs`.
- [X] T020 Rename `ServerLogsMenu` to `StructuredLogsMenu`, set label `Structured Logs`, route `diagnostics/structured-logs`, and group `Diagnostics` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Menu/StructuredLogsMenu.cs`.

**Checkpoint**: Renamed module identity, feature gate, REST client, SignalR observer, registrations, and menu compile.

---

## Phase 3: User Story 1 - Navigate to Structured Logs (Priority: P1) MVP

**Goal**: Studio exposes a diagnostics Structured Logs page only when the renamed backend feature is available.

**Independent Test**: Run Studio against a backend/mock advertising `Elsa.Diagnostics.StructuredLogs` and verify route, menu, page title, feature gate, API client, SignalR client, and assets use diagnostics structured logs naming.

### Tests for User Story 1

- [X] T021 [P] [US1] Rename and update mapper test project references in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/Elsa.Studio.Diagnostics.StructuredLogs.Tests.csproj`.
- [X] T022 [P] [US1] Rename `ServerLogFilterMapperTests` to `StructuredLogFilterMapperTests` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/StructuredLogFilterMapperTests.cs`.

### Implementation for User Story 1

- [X] T023 [US1] Rename `UI/Pages/ServerLogs.razor` to `UI/Pages/StructuredLogs.razor` and change route/page title/heading to Structured Logs.
- [X] T024 [US1] Rename `UI/Pages/ServerLogs.razor.cs` to `UI/Pages/StructuredLogs.razor.cs` and update class/fields/constants away from server-log identity where active.
- [X] T025 [US1] Import renamed JS module path `_content/Elsa.Studio.Diagnostics.StructuredLogs/structuredLogs.js` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor.cs`.
- [X] T026 [US1] Replace host static CSS links with `_content/Elsa.Studio.Diagnostics.StructuredLogs/structuredLogs.css` in `src/hosts/Elsa.Studio.Host.Server/Pages/_Host.cshtml` and `src/hosts/Elsa.Studio.Host.Wasm/wwwroot/index.html`.
- [X] T027 [US1] Update workflow instance link from `/server-logs?...` to `/diagnostics/structured-logs?...` in `src/modules/Elsa.Studio.Workflows/Components/WorkflowInstanceViewer/Components/WorkflowInstanceDetails.razor.cs`.
- [X] T028 [US1] Update module README to `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/README.md` with structured logs wording and future Console Logs/OpenTelemetry separation.

**Checkpoint**: User Story 1 is functional and testable as a renamed diagnostics navigation/page slice.

---

## Phase 4: User Story 2 - Inspect semantic log records (Priority: P2)

**Goal**: Operators can inspect rendered message, message template, properties, scopes, exception details, source metadata, trace/span IDs, event ID/name, workflow, tenant, correlation, and raw JSON/copy output.

**Independent Test**: Mock recent/live structured log events with templates, properties, scopes, exceptions, trace/span IDs, workflow IDs, and sources; verify rows and details render fields with copy actions and no overflow.

### Tests for User Story 2

- [X] T029 [P] [US2] Add event ID/name, message template, and span ID mapper coverage in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/StructuredLogFilterMapperTests.cs`.
- [ ] T030 [P] [US2] Add copy/raw JSON formatting tests if page helpers are extractable in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/StructuredLogFormattingTests.cs`.

### Implementation for User Story 2

- [X] T031 [US2] Add `EventId`, `EventName`, `MessageTemplate`, and `SpanId` to `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Models/StructuredLogEvent.cs`.
- [X] T032 [US2] Change scopes model to support structured scope values distinctly from properties in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Models/StructuredLogEvent.cs`.
- [X] T033 [US2] Add span ID to `StructuredLogFilter` and filter mapper in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Models/StructuredLogFilter.cs` and `Services/StructuredLogFilterMapper.cs`.
- [X] T034 [US2] Add row trace/correlation hint and event ID/name rendering in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor`.
- [X] T035 [US2] Add a selected-row details panel/drawer to `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor`.
- [X] T036 [US2] Implement selected details state, detail copy helpers, and raw JSON serialization in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor.cs`.
- [X] T037 [US2] Render message template, properties, scopes, exception details, source metadata, trace/span IDs, workflow IDs, tenant, and correlation in the details panel.
- [X] T038 [US2] Update CSS classes in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/wwwroot/structuredLogs.css` so long messages, properties, scope values, source names, and stack traces wrap/scroll without layout overflow.
- [X] T039 [US2] Replace remaining active UI wording that says server logs, live log stream, or console-like language with structured logs wording.

**Checkpoint**: User Stories 1 and 2 work together as a named Structured Logs page with semantic detail inspection.

---

## Phase 5: User Story 3 - Filter and correlate structured logs (Priority: P3)

**Goal**: Operators can filter structured logs by level, category, text, source, workflow context, tenant, correlation ID, trace ID, span ID, and time range.

**Independent Test**: Use mocked REST and SignalR contracts with multiple levels, categories, sources, workflow IDs, trace IDs, span IDs, and properties; verify filters update recent queries, live subscriptions, URL state, and visible rows.

### Tests for User Story 3

- [X] T040 [P] [US3] Add span ID and exact levels copy tests in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/StructuredLogFilterMapperTests.cs`.
- [ ] T041 [P] [US3] Add URL filter state tests if page state is extractable in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/StructuredLogUrlStateTests.cs`.

### Implementation for User Story 3

- [X] T042 [US3] Add URL query parsing and writing for `categoryPrefix`, `tenantId`, `workflowDefinitionId`, `traceId`, `spanId`, `correlationId`, `from`, and `to` in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor.cs`.
- [X] T043 [US3] Add filter controls for category, tenant, workflow definition/instance, trace, span, correlation, and time range in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor`.
- [X] T044 [US3] Ensure `RefreshFilterAsync` updates recent REST requests and live SignalR subscriptions with all filter fields in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor.cs`.
- [X] T045 [US3] Keep merged source view as `null SourceId` and individual source filtering in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor.cs`.
- [X] T046 [US3] Preserve source health indicators for connected, stale, disconnected, and unknown sources in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor`.
- [X] T047 [US3] Make trace/span IDs copyable and represent future OpenTelemetry deep-link placeholders without requiring an OpenTelemetry module in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor`.

**Checkpoint**: All stories are independently functional with diagnostics route, structured detail inspection, and correlation filters.

---

## Phase 6: Polish, Docs, and Verification

**Purpose**: Remove stale identity, document the module, and validate targeted builds/tests.

- [X] T048 Search `src` and `Elsa.Studio.sln` for active `Elsa.Studio.ServerLogs`, `ServerLog`, `ServerLogs`, `server-logs`, and `serverLogs` references and update remaining module-owned references.
- [X] T049 Search module UI/docs for console/stdout/stderr wording and keep only explicit future-module separation where appropriate in `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/README.md`.
- [X] T050 Run `dotnet test src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/Elsa.Studio.Diagnostics.StructuredLogs.Tests.csproj`.
- [X] T051 Run `dotnet build Elsa.Studio.sln`.
- [X] T052 Update this task list checkboxes as implementation completes in `specs/004-diagnostics-structured-logs/tasks.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1; blocks story work.
- **Phase 3 US1**: Depends on Phase 2; MVP route/menu/identity slice.
- **Phase 4 US2**: Depends on Phase 3 page and models.
- **Phase 5 US3**: Depends on Phase 4 model/filter expansion but can overlap in different files after foundational rename.
- **Phase 6 Polish**: Depends on selected story work.

### User Story Dependencies

- **US1 (P1)**: First executable product slice; no dependency on US2/US3.
- **US2 (P2)**: Builds on renamed page/model structures to add semantic inspection.
- **US3 (P3)**: Builds on renamed filter mapper/page structures and adds correlation filters.

### Parallel Opportunities

- T001 through T008 are mechanical renames but should be coordinated to keep the tree buildable.
- T011 through T014 can be prepared in parallel once paths are renamed.
- T021 and T022 can run in parallel with US1 UI work.
- T029 and T030 can run in parallel with details UI implementation.
- T040 and T041 can run in parallel with filter control implementation.

## Parallel Example: User Story 2

```text
Task: "Add event ID/name, message template, and span ID mapper coverage in src/modules/Elsa.Studio.Diagnostics.StructuredLogs.Tests/StructuredLogFilterMapperTests.cs"
Task: "Add a selected-row details panel/drawer to src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor"
Task: "Update CSS classes in src/modules/Elsa.Studio.Diagnostics.StructuredLogs/wwwroot/structuredLogs.css"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 only.
3. Validate route/menu/feature gate/host asset references against a backend advertising `Elsa.Diagnostics.StructuredLogs`.
4. Continue with semantic details and filters.

### Incremental Delivery

1. US1: diagnostics identity, route, menu, feature gate, host/bundle references.
2. US2: structured record model fields and details panel.
3. US3: full correlation filters, URL state, source health, trace/span copy affordances.
4. Polish: stale reference cleanup, docs, tests, build.

## Notes

- Do not modify `elsa-core`; this worker owns Studio only.
- Consistent breaking renames are acceptable because this feature branch is unpublished.
- Preserve bounded row behavior and disposal semantics while renaming.
- Keep `/server-logs` only as a redirect/development bookmark aid if it does not preserve stale active identity.
