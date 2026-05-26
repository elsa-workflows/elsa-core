# Tasks: Diagnostics OpenTelemetry

**Input**: Design documents from `/specs/008-diagnostics-otel/`  
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: Required for ingestion, normalization, redaction, bounded storage, permissions, live updates, collector configuration, and workflow export-to-collector timing.

## Phase 1: Setup

- [X] T001 Create Core project skeleton at `src/modules/Elsa.Diagnostics.OpenTelemetry/Elsa.Diagnostics.OpenTelemetry.csproj`
- [X] T002 Create unit test project at `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Elsa.Diagnostics.OpenTelemetry.UnitTests.csproj`
- [X] T003 Create integration test project at `test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/Elsa.Diagnostics.OpenTelemetry.IntegrationTests.csproj`
- [X] T004 Add project and test projects to `Elsa.sln`
- [X] T005 [P] Add initial module README at `src/modules/Elsa.Diagnostics.OpenTelemetry/README.md`

---

## Phase 2: Foundational

- [X] T006 Create telemetry resource/span/trace/metric/log/storage models in `src/modules/Elsa.Diagnostics.OpenTelemetry/Models`
- [X] T007 Create filter/result DTOs for resources, traces, metrics, logs, and collector configuration in `src/modules/Elsa.Diagnostics.OpenTelemetry/Models`
- [X] T008 Create provider, store, live feed, ingestor, redactor, and source registry contracts in `src/modules/Elsa.Diagnostics.OpenTelemetry/Contracts`
- [X] T009 Create options in `src/modules/Elsa.Diagnostics.OpenTelemetry/Options/OpenTelemetryDiagnosticsOptions.cs` with configurable capacity defaults, ingestion security, and live subscriber queue settings
- [X] T010 Create permission constants in `src/modules/Elsa.Diagnostics.OpenTelemetry/Permissions/OpenTelemetryPermissions.cs`
- [X] T011 Create feature and shell feature in `src/modules/Elsa.Diagnostics.OpenTelemetry/Features/OpenTelemetryFeature.cs` and `src/modules/Elsa.Diagnostics.OpenTelemetry/ShellFeatures/OpenTelemetryFeature.cs`
- [X] T012 Create DI and endpoint mapping extensions in `src/modules/Elsa.Diagnostics.OpenTelemetry/Extensions`
- [X] T013 Add README note that historical `Elsa.OpenTelemetry` producer middleware is not ported in v1 in `src/modules/Elsa.Diagnostics.OpenTelemetry/README.md`

---

## Phase 3: User Story 1 - Collect OpenTelemetry from Elsa services (Priority: P1) MVP

**Goal**: Receive OTLP payloads, normalize, redact, store bounded telemetry, and expose basic diagnostics queries.

**Independent Test**: Ingest OTLP traces/metrics/logs and run an Elsa workflow export-to-collector integration test.

- [X] T014 [P] [US1] Add OTLP trace normalization tests in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpTraceNormalizerTests.cs`
- [X] T015 [P] [US1] Add OTLP metric normalization tests in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpMetricNormalizerTests.cs`
- [X] T016 [P] [US1] Add OTLP log normalization tests in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Ingestion/OtlpLogNormalizerTests.cs`
- [X] T017 [P] [US1] Add redaction tests in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Services/OpenTelemetryRedactorTests.cs`
- [X] T018 [P] [US1] Add bounded store/drop-count tests for default capacities, drop-oldest overflow, and deterministic query limits in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Providers/InMemoryOpenTelemetryStoreTests.cs`
- [ ] T019 [P] [US1] Add HTTP/protobuf ingestion and workflow export-to-collector timing tests that assert existing `Elsa.Workflows` tags are preserved in `test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/OtlpHttpIngestionTests.cs`
- [X] T020 [US1] Implement HTTP/protobuf request parsing in `src/modules/Elsa.Diagnostics.OpenTelemetry/Ingestion/HttpProtobuf`
- [X] T021 [US1] Implement shared OTLP normalization services in `src/modules/Elsa.Diagnostics.OpenTelemetry/Ingestion`
- [X] T022 [US1] Implement redactor in `src/modules/Elsa.Diagnostics.OpenTelemetry/Services/OpenTelemetryRedactor.cs`
- [X] T023 [US1] Implement source registry in `src/modules/Elsa.Diagnostics.OpenTelemetry/Services/OpenTelemetrySourceRegistry.cs`
- [X] T024 [US1] Implement bounded in-memory store in `src/modules/Elsa.Diagnostics.OpenTelemetry/Providers/InMemory/InMemoryOpenTelemetryStore.cs`
- [X] T025 [US1] Implement provider facade in `src/modules/Elsa.Diagnostics.OpenTelemetry/Services/DefaultOpenTelemetryProvider.cs`
- [X] T026 [US1] Implement HTTP/protobuf endpoint mapping in `src/modules/Elsa.Diagnostics.OpenTelemetry/Extensions/EndpointRouteBuilderExtensions.cs`

---

## Phase 4: User Story 2 - Serve Trace Investigation APIs (Priority: P1)

**Goal**: Provide authenticated trace search/detail APIs and live updates.

**Independent Test**: Query traces by service/resource/trace/workflow/status/time and retrieve ordered span detail.

- [X] T027 [P] [US2] Add trace search/detail provider tests in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Providers/OpenTelemetryTraceQueryTests.cs`
- [ ] T028 [P] [US2] Add API authorization tests in `test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/OpenTelemetryAuthorizationTests.cs`
- [ ] T029 [P] [US2] Add SignalR hub tests for authorization, filter updates, per-subscriber queue overflow, drop-oldest behavior, and dropped-update diagnostics in `test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/OpenTelemetryHubTests.cs`
- [X] T030 [US2] Implement trace search and trace detail query logic in `src/modules/Elsa.Diagnostics.OpenTelemetry/Providers/InMemory/InMemoryOpenTelemetryStore.cs`
- [X] T031 [US2] Implement resource search endpoint in `src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Resources/Endpoint.cs`
- [X] T032 [US2] Implement trace search and detail endpoints in `src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Traces/Endpoint.cs` and `src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Trace/Endpoint.cs`
- [X] T033 [US2] Implement live feed in `src/modules/Elsa.Diagnostics.OpenTelemetry/Providers/InMemory/InMemoryOpenTelemetryLiveFeed.cs`
- [X] T034 [US2] Implement SignalR hub in `src/modules/Elsa.Diagnostics.OpenTelemetry/RealTime/OpenTelemetryHub.cs`

---

## Phase 5: User Story 3 - Serve Metrics and OTLP Logs (Priority: P2)

**Goal**: Provide bounded metric and OTLP log search APIs with overflow diagnostics.

**Independent Test**: Ingest metrics/logs, query by filters, and verify dropped counts.

- [X] T035 [P] [US3] Add metric query tests in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Providers/OpenTelemetryMetricQueryTests.cs`
- [X] T036 [P] [US3] Add OTLP log query tests in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Providers/OtlpLogQueryTests.cs`
- [X] T037 [US3] Implement metric instrument and point query support in `src/modules/Elsa.Diagnostics.OpenTelemetry/Providers/InMemory/InMemoryOpenTelemetryStore.cs`
- [X] T038 [US3] Implement OTLP log query support in `src/modules/Elsa.Diagnostics.OpenTelemetry/Providers/InMemory/InMemoryOpenTelemetryStore.cs`
- [X] T039 [US3] Implement metrics endpoint in `src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Metrics/Endpoint.cs`
- [X] T040 [US3] Implement OTLP logs endpoint in `src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Logs/Endpoint.cs`
- [X] T041 [US3] Implement storage diagnostics endpoint in `src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/Storage/Endpoint.cs`

---

## Phase 6: User Story 4 - Expose Collector Configuration and Secure Ingestion (Priority: P3)

**Goal**: Provide collector configuration metadata and protect non-loopback ingestion.

**Independent Test**: Query configuration and verify API key enforcement.

- [X] T042 [P] [US4] Add collector configuration tests in `test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Services/CollectorConfigurationTests.cs`
- [ ] T043 [P] [US4] Add ingestion security tests for loopback-only no-key development, non-loopback request rejection without API key, and non-secret collector metadata in `test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/OtlpIngestionSecurityTests.cs`
- [X] T044 [US4] Implement collector configuration provider in `src/modules/Elsa.Diagnostics.OpenTelemetry/Services/CollectorConfigurationProvider.cs`
- [X] T045 [US4] Implement collector configuration endpoint in `src/modules/Elsa.Diagnostics.OpenTelemetry/Endpoints/OpenTelemetry/CollectorConfiguration/Endpoint.cs`
- [X] T046 [US4] Implement API key header validation in `src/modules/Elsa.Diagnostics.OpenTelemetry/Ingestion`
- [ ] T047 [US4] Add option-gated gRPC wrappers and disabled metadata handling in `src/modules/Elsa.Diagnostics.OpenTelemetry/Ingestion/Grpc`
- [X] T048 [US4] Update workflow OpenTelemetry docs in `doc/wiki/opentelemetry-workflows.md`

---

## Phase 7: Polish and Verification

- [X] T049 [P] Update module README with routes, permissions, security, scope boundaries, and historical extension decision in `src/modules/Elsa.Diagnostics.OpenTelemetry/README.md`
- [X] T050 [P] Verify no production code references historical producer middleware by running `rg "Elsa.OpenTelemetry|UseWorkflowExecutionTracing|UseActivityExecutionTracing" src test`
- [X] T051 Run `dotnet test test/unit/Elsa.Diagnostics.OpenTelemetry.UnitTests/Elsa.Diagnostics.OpenTelemetry.UnitTests.csproj`
- [ ] T052 Run `dotnet test test/integration/Elsa.Diagnostics.OpenTelemetry.IntegrationTests/Elsa.Diagnostics.OpenTelemetry.IntegrationTests.csproj`
- [ ] T053 Run `dotnet build Elsa.sln`

## Dependencies

- Phase 1 precedes all work.
- Phase 2 blocks all user stories.
- US1 is the MVP backend collector slice.
- US2 can begin after Phase 2 with seeded store data; full validation depends on US1.
- US3 depends on US1 storage and normalization.
- US4 depends on endpoint mapping from US1.

## Notes

- Keep Studio UI tasks in `elsa-studio/specs/008-diagnostics-otel/tasks.md`.
- Do not port historical `Elsa.OpenTelemetry` middleware in this feature.
- Durable OTEL persistence and vendor exporters are out of scope.
