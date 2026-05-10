# Tasks: Diagnostics Structured Logs

**Input**: Design documents from `/specs/004-diagnostics-structured-logs/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts), [quickstart.md](./quickstart.md)

**Tests**: Included because the specification defines independent tests for renamed module identity, semantic capture, redaction, scopes, filtering, live streams, and docs.

**Organization**: Tasks are grouped by user story so each increment can be validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks after prerequisites are satisfied.
- **[Story]**: User story label from [spec.md](./spec.md).
- Every task includes the primary file path to edit or create.

## Phase 1: Setup (Rename Skeleton)

**Purpose**: Move the existing unpublished module and tests to the diagnostics structured logs identity.

- [X] T001 Rename `src/modules/Elsa.ServerLogs/Elsa.ServerLogs.csproj` to `src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj`.
- [X] T002 Rename `test/unit/Elsa.ServerLogs.UnitTests/Elsa.ServerLogs.UnitTests.csproj` to `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Elsa.Diagnostics.StructuredLogs.UnitTests.csproj`.
- [X] T003 Rename `test/integration/Elsa.ServerLogs.IntegrationTests/Elsa.ServerLogs.IntegrationTests.csproj` to `test/integration/Elsa.Diagnostics.StructuredLogs.IntegrationTests/Elsa.Diagnostics.StructuredLogs.IntegrationTests.csproj`.
- [X] T004 Update `Elsa.sln` project names and paths for the renamed module and test projects.
- [X] T005 Update sample host references in `src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj`.

---

## Phase 2: Foundational (Blocking Rename Prerequisites)

**Purpose**: Rename shared contracts, models, options, permissions, services, and folders before story-specific behavior changes.

**Critical**: No story work should begin until these shared names compile.

- [X] T006 Rename namespaces from `Elsa.ServerLogs` to `Elsa.Diagnostics.StructuredLogs` across `src/modules/Elsa.Diagnostics.StructuredLogs`.
- [X] T007 [P] Rename model types in `src/modules/Elsa.Diagnostics.StructuredLogs/Models` from `ServerLog*` to `StructuredLog*`.
- [X] T008 [P] Rename provider contracts in `src/modules/Elsa.Diagnostics.StructuredLogs/Contracts` from `IServerLog*` to `IStructuredLog*`.
- [X] T009 [P] Rename option type `ServerLogStreamingOptions` to `StructuredLogsOptions` in `src/modules/Elsa.Diagnostics.StructuredLogs/Options/StructuredLogsOptions.cs`.
- [X] T010 [P] Rename permission type and value in `src/modules/Elsa.Diagnostics.StructuredLogs/Permissions/StructuredLogsPermissions.cs`.
- [X] T011 Rename service, logging, provider, and real-time types in `src/modules/Elsa.Diagnostics.StructuredLogs` to structured-log names.
- [X] T012 Update source code references after renames in `src/modules/Elsa.Diagnostics.StructuredLogs`.
- [X] T013 Update test namespaces and project references in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests` and `test/integration/Elsa.Diagnostics.StructuredLogs.IntegrationTests`.

**Checkpoint**: Renamed projects and test projects should compile far enough to expose behavior-specific failures.

---

## Phase 3: User Story 1 - Install a clearly named structured logs module (Priority: P1) MVP

**Goal**: Hosts enable a diagnostics structured logs module whose APIs, shell feature, installed feature name, routes, and docs no longer imply raw server console logs.

**Independent Test**: Build a host and tests using `Elsa.Diagnostics.StructuredLogs`, `StructuredLogsFeature`, `UseStructuredLogs`, diagnostics routes, and diagnostics permission names.

### Tests for User Story 1

- [X] T014 [P] [US1] Update module identity assertions in `test/integration/Elsa.Diagnostics.StructuredLogs.IntegrationTests/StructuredLogsModuleTests.cs`.
- [X] T015 [P] [US1] Add route and permission naming assertions in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/StructuredLogsNamingTests.cs`.
- [X] T016 [P] [US1] Add shell feature option binding assertions in `test/integration/Elsa.Diagnostics.StructuredLogs.IntegrationTests/StructuredLogsModuleTests.cs`.

### Implementation for User Story 1

- [X] T017 [US1] Rename Core feature to `StructuredLogsFeature` in `src/modules/Elsa.Diagnostics.StructuredLogs/Features/StructuredLogsFeature.cs`.
- [X] T018 [US1] Rename fluent module extension to `UseStructuredLogs` in `src/modules/Elsa.Diagnostics.StructuredLogs/Extensions/ModuleExtensions.cs`.
- [X] T019 [US1] Rename application/hub mapping extension to `UseStructuredLogs` and `MapStructuredLogsHub` in `src/modules/Elsa.Diagnostics.StructuredLogs/Extensions`.
- [X] T020 [US1] Move FastEndpoints folders/routes to diagnostics structured-log names in `src/modules/Elsa.Diagnostics.StructuredLogs/Endpoints/StructuredLogs`.
- [X] T021 [US1] Rename shell feature to diagnostics structured logs and keep bindable public option properties in `src/modules/Elsa.Diagnostics.StructuredLogs/ShellFeatures/StructuredLogsFeature.cs`.
- [X] T022 [US1] Update sample host wiring to `UseStructuredLogs` in `src/apps/Elsa.Server.Web/Program.cs`.
- [X] T023 [US1] Update package metadata in `src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj`.

**Checkpoint**: User Story 1 is functional through code-based and shell-based module activation.

---

## Phase 4: User Story 2 - Preserve semantic log data for inspection (Priority: P2)

**Goal**: Captured records include rendered messages, original templates, structured properties, active scopes, exceptions, source metadata, and trace/span IDs after redaction.

**Independent Test**: Emit `ILogger` calls with message templates, named arguments, scopes, exceptions, workflow/correlation context, and active `Activity`; verify captured semantic fields.

### Tests for User Story 2

- [X] T024 [P] [US2] Add message template capture tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Logging/StructuredLogLoggerProviderTests.cs`.
- [X] T025 [P] [US2] Add active scope capture tests in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Logging/StructuredLogLoggerProviderTests.cs`.
- [X] T026 [P] [US2] Update redaction tests for renamed scope/property models in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Redaction/StructuredLogRedactorTests.cs`.
- [X] T027 [P] [US2] Update filter/provider tests for renamed semantic fields in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests`.

### Implementation for User Story 2

- [X] T028 [US2] Implement `ISupportExternalScope` in `src/modules/Elsa.Diagnostics.StructuredLogs/Logging/StructuredLogLoggerProvider.cs`.
- [X] T029 [US2] Capture active scopes in `src/modules/Elsa.Diagnostics.StructuredLogs/Logging/StructuredLogLogger.cs`.
- [X] T030 [US2] Populate `MessageTemplate` from `{OriginalFormat}` in `src/modules/Elsa.Diagnostics.StructuredLogs/Logging/StructuredLogLogger.cs`.
- [X] T031 [US2] Keep `{OriginalFormat}` out of structured properties in `src/modules/Elsa.Diagnostics.StructuredLogs/Logging/StructuredLogLogger.cs`.
- [X] T032 [US2] Preserve redaction of messages, exceptions, scopes, and structured properties in `src/modules/Elsa.Diagnostics.StructuredLogs/Services/StructuredLogRedactor.cs`.
- [X] T033 [US2] Preserve recursion guard for internal structured-log categories in `src/modules/Elsa.Diagnostics.StructuredLogs/Logging/StructuredLogLogger.cs`.

**Checkpoint**: User Stories 1 and 2 work together with semantic template/scope capture and existing safety behavior.

---

## Phase 5: User Story 3 - Keep structured logs separate from console logs and telemetry exploration (Priority: P3)

**Goal**: Contracts and documentation make the module boundary clear: structured `ILogger` events only, not stdout/stderr capture or trace/metric exploration.

**Independent Test**: Review API routes, hub routes, models, permissions, docs, and feature metadata for structured-log naming and explicit out-of-scope notes.

### Tests for User Story 3

- [X] T034 [P] [US3] Add README boundary assertions or doc smoke coverage in `test/integration/Elsa.Diagnostics.StructuredLogs.IntegrationTests/StructuredLogsModuleTests.cs`.
- [X] T035 [P] [US3] Add route contract assertions for diagnostics paths in `test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/StructuredLogsNamingTests.cs`.

### Implementation for User Story 3

- [X] T036 [US3] Update README boundary language in `src/modules/Elsa.Diagnostics.StructuredLogs/README.md`.
- [X] T037 [US3] Ensure REST routes use `/diagnostics/structured-logs` in `src/modules/Elsa.Diagnostics.StructuredLogs/Endpoints/StructuredLogs`.
- [X] T038 [US3] Ensure SignalR route uses `/elsa/hubs/diagnostics/structured-logs` in `src/modules/Elsa.Diagnostics.StructuredLogs/Extensions/EndpointRouteBuilderExtensions.cs`.
- [X] T039 [US3] Ensure permission value is `read:diagnostics:structured-logs` in `src/modules/Elsa.Diagnostics.StructuredLogs/Permissions/StructuredLogsPermissions.cs`.
- [X] T040 [US3] Update Speckit quickstart validation notes in `specs/004-diagnostics-structured-logs/quickstart.md`.

**Checkpoint**: Structured logs, console streaming, and OpenTelemetry exploration have clean documented boundaries.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Search cleanup, validation, and implementation commit readiness.

- [X] T041 [P] Remove active `Elsa.ServerLogs` references from production source, tests, projects, README, and sample host wiring.
- [X] T042 [P] Remove active `ServerLogStreaming` references from production source, tests, projects, README, and sample host wiring.
- [X] T043 [P] Update generated XML include filters in renamed test `.csproj` files.
- [X] T044 Run `dotnet build src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj`.
- [X] T045 Run `dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Elsa.Diagnostics.StructuredLogs.UnitTests.csproj`.
- [X] T046 Run `dotnet test test/integration/Elsa.Diagnostics.StructuredLogs.IntegrationTests/Elsa.Diagnostics.StructuredLogs.IntegrationTests.csproj`.
- [X] T047 Run `rg "Elsa\\.ServerLogs|ServerLogStreaming|server-logs|read:server-logs" src test`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user stories.
- **Phase 3 US1**: Depends on Phase 2; MVP.
- **Phase 4 US2**: Depends on Phase 2 and can run after or alongside US1 implementation once names compile.
- **Phase 5 US3**: Depends on route/permission names from US1.
- **Phase 6 Polish**: Depends on selected story implementation.

### User Story Dependencies

- **US1 (P1)**: First executable slice; establishes final module identity.
- **US2 (P2)**: Builds on renamed logging provider and models.
- **US3 (P3)**: Builds on renamed routes, permissions, and docs.

### Parallel Opportunities

- T002 and T003 can run in parallel after T001.
- T007 through T010 can run in parallel once the module folder is renamed.
- US1 tests T014 through T016 can run in parallel.
- US2 tests T024 through T027 can run in parallel.
- US3 tests T034 and T035 can run in parallel.
- Polish checks T041 through T043 can run in parallel before builds/tests.

## Parallel Example: User Story 2

```text
Task: "Add message template capture tests in test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Logging/StructuredLogLoggerProviderTests.cs"
Task: "Add active scope capture tests in test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Logging/StructuredLogLoggerProviderTests.cs"
Task: "Update redaction tests for renamed scope/property models in test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Redaction/StructuredLogRedactorTests.cs"
Task: "Update filter/provider tests for renamed semantic fields in test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 only.
3. Build the module and verify code/shell activation names.
4. Continue to semantic capture after the renamed surface is stable.

### Incremental Delivery

1. US1: consistent diagnostics structured logs identity.
2. US2: message templates, scopes, and preserved structured data.
3. US3: documented module boundary and diagnostics contracts.
4. Polish: search cleanup, builds, and tests.

## Notes

- Do not modify the separate `elsa-studio` repository.
- Do not implement console stdout/stderr capture in this feature.
- Do not implement OpenTelemetry trace or metric exploration in this feature.
- Preserve bounded buffering, source behavior, redaction order, and dropped-event summaries.
