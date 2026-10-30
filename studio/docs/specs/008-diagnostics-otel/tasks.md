# Tasks: Diagnostics OpenTelemetry

**Input**: Design documents from `/specs/008-diagnostics-otel/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Required. The PRD calls for Core unit/integration tests and Studio mapper/service/UI verification tests where practical.

**Organization**: Tasks are grouped by user story so each slice can be implemented and validated independently after shared foundations are complete.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create module/test skeletons and wire build visibility without implementing behavior.

- [X] T001 Create Core project skeleton at `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Elsa.Diagnostics.OpenTelemetry.csproj`
- [X] T002 Create Core unit test project at `elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Elsa.Diagnostics.OpenTelemetry.UnitTests.csproj`
- [X] T003 Create Core integration test project at `elsa-core/test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/Elsa.Diagnostics.OpenTelemetry.IntegrationTests.csproj`
- [X] T004 Add Core project and test projects to `elsa-core/Elsa.sln`
- [X] T005 [P] Create Studio module skeleton at `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/Elsa.Studio.Diagnostics.OpenTelemetry.csproj`
- [X] T006 [P] Create Studio test project at `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/Elsa.Studio.Diagnostics.OpenTelemetry.Tests.csproj`
- [X] T007 Add Studio module and test project references to `elsa-studio/src/bundles/Elsa.Studio/Elsa.Studio.csproj`
- [X] T008 [P] Add initial README for Core module at `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/README.md`
- [X] T009 [P] Add initial README for Studio module at `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/README.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared contracts, options, permissions, and registration that all user stories depend on.

**CRITICAL**: No user story work should begin until this phase is complete.

- [X] T010 Create Core telemetry resource/span/trace/metric/log/storage models in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Models`
- [X] T011 Create Core filter/result DTOs for resources, traces, metrics, logs, and collector configuration in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Models`
- [X] T012 Create Core contracts `IOpenTelemetryIngestor`, `IOpenTelemetryProvider`, `IOpenTelemetryStore`, `IOpenTelemetryLiveFeed`, `IOpenTelemetryRedactor`, and `IOpenTelemetrySourceRegistry` in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Contracts`
- [X] T013 Create Core options in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Options/OpenTelemetryDiagnosticsOptions.cs`
- [X] T014 Create Core permission constants in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Permissions/OpenTelemetryPermissions.cs`
- [X] T015 Create Core feature and shell feature in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Features/OpenTelemetryFeature.cs` and `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/ShellFeatures/OpenTelemetryFeature.cs`
- [X] T016 Create Core DI and endpoint mapping extensions in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Extensions`
- [X] T017 [P] Create Studio API client interface in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/Client/IOpenTelemetryApi.cs`
- [X] T018 [P] Create Studio model DTOs matching Core contracts in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/Models`
- [X] T019 Create Studio contracts, feature, service registration, and menu provider in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry`
- [X] T020 [P] Add Studio module imports and static asset placeholders in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/_Imports.razor` and `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/wwwroot`
- [X] T021 Add explicit note in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/README.md` that historical `Elsa.OpenTelemetry` producer middleware is not ported in v1

**Checkpoint**: Module shells, contracts, permissions, and registration points exist in both repositories.

---

## Phase 3: User Story 1 - Collect OpenTelemetry from Elsa services (Priority: P1) MVP

**Goal**: Core receives OTLP telemetry, normalizes it, redacts it, stores it in bounded memory, and exposes authenticated diagnostics APIs.

**Independent Test**: Run Core tests that ingest representative OTLP trace/metric/log payloads, execute a workflow trace path, and query recent telemetry through provider/API contracts.

### Tests for User Story 1

- [X] T022 [P] [US1] Add unit tests for OTLP trace normalization in `elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpTraceNormalizerTests.cs`
- [X] T023 [P] [US1] Add unit tests for OTLP metric normalization in `elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpMetricNormalizerTests.cs`
- [X] T024 [P] [US1] Add unit tests for OTLP log normalization in `elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpLogNormalizerTests.cs`
- [X] T025 [P] [US1] Add unit tests for redaction before storage in `elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Services/OpenTelemetryRedactorTests.cs`
- [X] T026 [P] [US1] Add unit tests for bounded in-memory drop accounting in `elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Providers/InMemoryOpenTelemetryStoreTests.cs`
- [X] T027 [P] [US1] Add integration tests for HTTP/protobuf OTLP endpoints and end-to-end Elsa workflow export-to-collector timing in `elsa-core/test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/OtlpHttpIngestionTests.cs`
- [X] T028 [P] [US1] Add integration tests for OpenTelemetry diagnostics API authorization in `elsa-core/test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/OpenTelemetryAuthorizationTests.cs`

### Implementation for User Story 1

- [X] T029 [US1] Implement OTLP protobuf request parsing for traces, metrics, and logs in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Ingestion/HttpProtobuf`
- [X] T030 [US1] Implement shared OTLP normalization services in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Ingestion`
- [X] T031 [US1] Implement `IOpenTelemetryRedactor` in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Services/OpenTelemetryRedactor.cs`
- [X] T032 [US1] Implement resource identity registry in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Services/OpenTelemetrySourceRegistry.cs`
- [X] T033 [US1] Implement bounded in-memory store in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Providers/InMemory/InMemoryOpenTelemetryStore.cs`
- [X] T034 [US1] Implement provider facade in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Services/DefaultOpenTelemetryProvider.cs`
- [X] T035 [US1] Implement live feed and subscriber drop accounting in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Providers/InMemory/InMemoryOpenTelemetryLiveFeed.cs`
- [X] T036 [US1] Implement authenticated resource search endpoint in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Resources/Endpoint.cs`
- [X] T037 [US1] Implement authenticated trace search and trace detail endpoints in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Traces/Endpoint.cs` and `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Trace/Endpoint.cs`
- [X] T038 [US1] Implement authenticated metric, OTLP log, and storage diagnostics endpoints in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry`
- [X] T039 [US1] Implement HTTP/protobuf OTLP endpoint mapping in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Extensions/EndpointRouteBuilderExtensions.cs`
- [X] T040 [US1] Add option-gated gRPC ingestion service wrappers and disabled metadata handling in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Ingestion/Grpc`
- [X] T041 [US1] Wire Core services, options, endpoints, permissions, and shell feature in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Extensions`

**Checkpoint**: Core can collect and query recent OpenTelemetry resources, traces, metrics, and OTLP logs without Studio.

---

## Phase 4: User Story 2 - Investigate workflow traces in Studio (Priority: P1)

**Goal**: Studio exposes a feature-gated OpenTelemetry route with trace list, trace waterfall, span details, live status, and Structured Logs correlation.

**Independent Test**: Use mocked backend telemetry for a workflow trace with child activity spans, errors, and matching structured logs; verify route gating, trace list, detail waterfall, span details, and trace/span links.

### Tests for User Story 2

- [X] T042 [P] [US2] Add Studio filter mapper tests for trace filters in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/OpenTelemetryFilterMapperTests.cs`
- [X] T043 [P] [US2] Add Studio URL state mapper tests for tab, resource, service, trace, span, workflow, severity, status, text, time range, and live state in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/OpenTelemetryUrlStateMapperTests.cs`
- [X] T044 [P] [US2] Add Studio service tests for trace search/detail API calls in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/RemoteOpenTelemetryServiceTests.cs`
- [X] T045 [P] [US2] Add Studio trace waterfall model tests in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/TraceWaterfallLayoutTests.cs`
- [X] T046 [P] [US2] Add Core SignalR hub integration tests in `elsa-core/test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/OpenTelemetryHubTests.cs`

### Implementation for User Story 2

- [X] T047 [US2] Implement Core SignalR hub and client contract in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/RealTime/OpenTelemetryHub.cs`
- [X] T048 [US2] Implement Studio remote service in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/Services/RemoteOpenTelemetryService.cs`
- [X] T049 [US2] Implement Studio SignalR observer in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/Services/SignalROpenTelemetryObserver.cs`
- [X] T050 [US2] Implement Studio filter mapper and full URL state mapper in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/Services`
- [X] T051 [US2] Implement feature-gated OpenTelemetry page shell in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Pages/OpenTelemetry.razor`
- [X] T052 [US2] Implement Resources and Traces tabs plus `ResourcePicker.razor` in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI`
- [X] T053 [US2] Implement trace waterfall component in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Components/TraceWaterfall.razor`
- [X] T054 [US2] Implement span details drawer in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Components/SpanDetailsDrawer.razor`
- [X] T055 [US2] Add Structured Logs trace/span links and compatible Console Logs resource/time links in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Pages/OpenTelemetry.razor`
- [X] T056 [US2] Add OpenTelemetry deep-link actions from Structured Logs in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.StructuredLogs/UI/Pages/StructuredLogs.razor`
- [X] T057 [US2] Add responsive styling for trace list and waterfall in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Pages/OpenTelemetry.razor.css`

**Checkpoint**: Studio can investigate workflow traces and correlate them to Structured Logs against mocked or real Core APIs.

---

## Phase 5: User Story 3 - Monitor operational metrics (Priority: P2)

**Goal**: Operators can view recent OpenTelemetry metrics grouped by resource/instrument with bounded trend data.

**Independent Test**: Feed metrics for several resources and instruments; verify Core search results and Studio metric rendering with high-cardinality truncation.

### Tests for User Story 3

- [X] T058 [P] [US3] Add Core metric query tests in `elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Providers/OpenTelemetryMetricQueryTests.cs`
- [X] T059 [P] [US3] Add Studio metric mapper tests in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/MetricSeriesMapperTests.cs`
- [X] T060 [P] [US3] Add Studio metric view-state tests in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/MetricViewStateTests.cs`

### Implementation for User Story 3

- [X] T061 [US3] Add metric instrument and point filtering support in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Providers/InMemory/InMemoryOpenTelemetryStore.cs`
- [X] T062 [US3] Add metric overflow diagnostics to `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Models/OpenTelemetryStorageDiagnostics.cs`
- [X] T063 [US3] Implement Metrics tab in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Pages/OpenTelemetry.razor`
- [X] T064 [US3] Implement metric series table in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Components/MetricSeriesTable.razor`
- [X] T065 [US3] Add metric live-update handling in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/Services/SignalROpenTelemetryObserver.cs`

**Checkpoint**: Metric instruments and recent bounded points are visible and filterable in Studio.

---

## Phase 6: User Story 4 - Configure external OTLP senders (Priority: P3)

**Goal**: Developers can discover active collector endpoints and configure .NET or polyglot senders with standard OTEL environment variables.

**Independent Test**: Query collector configuration from Core, display it in Studio setup, copy environment variable examples, and send telemetry from a sample sender.

### Tests for User Story 4

- [X] T066 [P] [US4] Add Core collector configuration tests for HTTP metadata, nullable/disabled gRPC metadata, and required headers in `elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Services/CollectorConfigurationTests.cs`
- [X] T067 [P] [US4] Add Core non-loopback API key enforcement tests in `elsa-core/test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/OtlpIngestionSecurityTests.cs`
- [X] T068 [P] [US4] Add Studio setup view mapper tests in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/CollectorConfigurationViewModelTests.cs`

### Implementation for User Story 4

- [X] T069 [US4] Implement collector configuration service in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Services/CollectorConfigurationProvider.cs`
- [X] T070 [US4] Implement collector configuration endpoint in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/CollectorConfiguration/Endpoint.cs`
- [X] T071 [US4] Implement OTLP API key header validation in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/Ingestion`
- [X] T072 [US4] Implement Setup tab in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Pages/OpenTelemetry.razor`
- [X] T073 [US4] Add setup copy helpers in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/wwwroot/openTelemetry.js`
- [X] T074 [US4] Add polyglot and .NET setup examples to `elsa-core/doc/wiki/opentelemetry-workflows.md`

**Checkpoint**: Setup metadata is visible in Studio and secure enough for non-loopback ingestion.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, verification, and cleanup across Core and Studio.

- [X] T075 [P] Update Core README with scope boundaries, routes, permissions, and historical extension decision in `elsa-core/src/modules/Elsa.Diagnostics.OpenTelemetry/README.md`
- [X] T076 [P] Update Studio README with feature gating, route, UI states, and cross-links in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/README.md`
- [X] T077 [P] Add Core quickstart validation notes to `elsa-core/doc/wiki/opentelemetry-workflows.md`
- [X] T078 [P] Verify no production code references the historical `Elsa.OpenTelemetry` module by running `rg "Elsa.OpenTelemetry|UseWorkflowExecutionTracing|UseActivityExecutionTracing" elsa-core elsa-studio`
- [X] T079 Run Core OpenTelemetry unit tests with `dotnet test elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Elsa.Diagnostics.OpenTelemetry.UnitTests.csproj`
- [X] T080 Run Core OpenTelemetry integration tests with `dotnet test elsa-core/test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/Elsa.Diagnostics.OpenTelemetry.IntegrationTests.csproj`
- [X] T081 Run Studio OpenTelemetry tests with `dotnet test elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/Elsa.Studio.Diagnostics.OpenTelemetry.Tests.csproj`
- [X] T082 Build Core solution with `dotnet build elsa-core/Elsa.sln`
- [X] T083 Build Studio solution with `dotnet build elsa-studio/Elsa.Studio.sln`
- [X] T084 Manually verify Studio route states for enabled, missing feature, unauthorized, disconnected, empty, overflow, and live telemetry states in `elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry/UI/Pages/OpenTelemetry.razor`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **US1 Collect OpenTelemetry (Phase 3)**: Depends on Foundational; MVP backend slice.
- **US2 Investigate Traces (Phase 4)**: Depends on Foundational and can use mocked APIs, but full end-to-end validation depends on US1 APIs.
- **US3 Monitor Metrics (Phase 5)**: Depends on Foundational and metric parts of US1.
- **US4 Configure Senders (Phase 6)**: Depends on Foundational and endpoint mapping from US1.
- **Polish (Phase 7)**: Depends on all desired user stories.

### User Story Dependencies

- **User Story 1 (P1)**: MVP backend collection; no dependency on Studio.
- **User Story 2 (P1)**: Can begin after Foundational with mocked API contracts; complete validation benefits from US1.
- **User Story 3 (P2)**: Can begin after Foundational; complete validation depends on US1 metric ingestion.
- **User Story 4 (P3)**: Can begin after Foundational; complete validation depends on US1 collector endpoints.

### Parallel Opportunities

- T005, T006, T008, and T009 can run in parallel with Core setup tasks.
- T017 and T018 can run in parallel with Core foundational contracts once DTO names are agreed.
- T022 through T028 can be written in parallel.
- T042 through T046 can be written in parallel.
- T058 through T060 can be written in parallel.
- T066 through T068 can be written in parallel.
- T075 through T077 can be updated in parallel after implementation settles.

---

## Parallel Example: User Story 1

```text
Task: "Add unit tests for OTLP trace normalization in elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpTraceNormalizerTests.cs"
Task: "Add unit tests for OTLP metric normalization in elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpMetricNormalizerTests.cs"
Task: "Add unit tests for OTLP log normalization in elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpLogNormalizerTests.cs"
Task: "Add unit tests for bounded in-memory drop accounting in elsa-core/test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Providers/InMemoryOpenTelemetryStoreTests.cs"
```

## Parallel Example: User Story 2

```text
Task: "Add Studio filter mapper tests for trace filters in elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/OpenTelemetryFilterMapperTests.cs"
Task: "Add Studio URL state mapper tests for tab/resource/service/trace/span/workflow/severity/status/text/time/live state in elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/OpenTelemetryUrlStateMapperTests.cs"
Task: "Add Studio service tests for trace search/detail API calls in elsa-studio/src/modules/Elsa.Studio.Diagnostics.OpenTelemetry.Tests/RemoteOpenTelemetryServiceTests.cs"
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 for Core collection and read APIs.
3. Validate Core independently with OTLP ingestion and API tests.
4. Complete the trace-focused parts of Phase 4 for Studio investigation.

### Incremental Delivery

1. Core collector/API MVP: resources, traces, spans, bounded store, redaction, permissions.
2. Studio trace MVP: gated route, trace list, trace detail, live status, Structured Logs links.
3. Metrics: metric ingestion/search plus Studio metrics tab.
4. Setup/security: collector configuration view and non-loopback protection.
5. Documentation and broader verification.

### Notes

- Keep historical `Elsa.OpenTelemetry` producer middleware out of this implementation.
- Revisit custom error-span handlers only after collector and Studio workflows are stable.
- Avoid durable OTEL persistence, vendor exporters, and app-launcher behavior in this feature.
