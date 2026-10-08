# Tasks: Extensible Activity Output Converters

**Input**: Design documents from `specs/012-output-converters/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, and `contracts/`

**Tests**: Required by FR-042 and the repository constitution. Test tasks precede their corresponding implementation.

**Organization**: Tasks are grouped by independently testable user story.

## Phase 1: Setup

**Purpose**: Add the one standards dependency and establish feature test locations.

- [x] T001 Add a centrally versioned JsonSchema.Net dependency in `Directory.Packages.props` and reference it from `src/modules/Elsa.Workflows.Core/Elsa.Workflows.Core.csproj`
- [x] T002 [P] Add output-converter test folders and shared test fixtures under `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/`
- [x] T003 [P] Add Studio output-converter test folder under `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows.Tests/OutputConverters/`

---

## Phase 2: Foundational Contracts and Persistence

**Purpose**: Establish public models, registration, serialization, and safe metadata used by every story.

**Critical**: User-story work begins after these contracts and round-trip tests pass.

- [x] T004 [P] Add failing normal and omitted-configuration serialization tests in `test/unit/Elsa.Workflows.Core.UnitTests/Serialization/Converters/OutputJsonConverterTests.cs`
- [x] T005 [P] Add failing synthetic-output serialization tests in `test/unit/Elsa.Workflows.Core.UnitTests/Serialization/Helpers/SyntheticPropertiesWriterTests.cs`
- [x] T006 [P] Add failing registry identity and compatibility tests in `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/OutputConverterRegistryTests.cs`
- [x] T007 Add Converter Configuration, Descriptor, Conversion Context, Registration, and safe error metadata models under `src/modules/Elsa.Workflows.Core/Models/`
- [x] T008 [P] Add `IOutputConverter`, `IOutputConverterRegistry`, `IOutputConverterInvoker`, destination resolver, and settings validator contracts under `src/modules/Elsa.Workflows.Core/Contracts/`
- [x] T009 [P] Add failure-stage enum and privacy-safe `OutputConversionException` under `src/modules/Elsa.Workflows.Core/Enums/` and `src/modules/Elsa.Workflows.Core/Exceptions/`
- [x] T010 Implement strict descriptor registry and keyed DI registration extensions under `src/modules/Elsa.Workflows.Core/Services/OutputConverterRegistry.cs` and `src/modules/Elsa.Workflows.Core/Extensions/OutputConverterServiceCollectionExtensions.cs`
- [x] T011 Register registry, resolver, validator, and invoker infrastructure in both `src/modules/Elsa.Workflows.Core/Features/WorkflowsFeature.cs` and `src/modules/Elsa.Workflows.Core/ShellFeatures/WorkflowsFeature.cs`
- [x] T012 Add optional converter configuration to `src/modules/Elsa.Workflows.Core/Models/Output.cs`
- [x] T013 Update normal and synthetic output JSON round-tripping in `src/modules/Elsa.Workflows.Core/Serialization/Converters/OutputJsonConverter.cs` and `src/modules/Elsa.Workflows.Core/Serialization/Helpers/SyntheticPropertiesWriter.cs`
- [x] T014 Update API-client activity output round-tripping in `src/clients/Elsa.Api.Client/Shared/Models/ActivityOutput.cs`
- [x] T015 Run the foundational Core serialization and registry tests in `test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj`

**Checkpoint**: Converter configuration round-trips, omitted JSON is unchanged, and registrations are stable and discoverable.

---

## Phase 3: User Story 1 - Deliver a Converted Bound Value (Priority: P1)

**Goal**: Convert only the destination value while preserving native activity-output observation and the default hot path.

**Independent Test**: Execute variable and workflow-output bindings with and without a converter and compare Bound Values with native output records.

### Tests

- [x] T016 [P] [US1] Add failing destination resolution, source/result compatibility, and nullability tests in `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/OutputBindingDestinationResolverTests.cs`
- [x] T017 [P] [US1] Add failing scoped invocation, settings, null bypass, result validation, and atomicity tests in `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/OutputConverterInvokerTests.cs`
- [x] T018 [P] [US1] Add failing native-register versus Bound Value and no-converter lookup tests in `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/ActivityExecutionContextOutputConversionTests.cs`
- [x] T019 [P] [US1] Add failing variable/workflow-output component scenarios in `test/component/Elsa.Workflows.ComponentTests/Scenarios/OutputConverters/OutputConverterTests.cs`

### Implementation

- [x] T020 [US1] Implement runtime/static destination resolution in `src/modules/Elsa.Workflows.Core/Services/OutputBindingDestinationResolver.cs`
- [x] T021 [US1] Implement standards and custom settings validation in `src/modules/Elsa.Workflows.Core/Services/OutputConverterSettingsValidator.cs`
- [x] T022 [US1] Implement scoped resolution, compatibility checks, invocation, result checks, and privacy-safe wrapping in `src/modules/Elsa.Workflows.Core/Services/OutputConverterInvoker.cs`
- [x] T023 [US1] Add a binder-specific already-converted destination write in `src/modules/Elsa.Workflows.Core/Extensions/ExpressionExecutionContextExtensions.cs`
- [x] T024 [US1] Orchestrate configured conversion while preserving the unchanged default branch in `src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs`
- [x] T025 [US1] Run User Story 1 Core unit and component tests

**Checkpoint**: Valid bindings receive converted values, observation surfaces retain native values, configured failures are atomic, and unconfigured bindings use the old path.

---

## Phase 4: User Story 2 - Register a Reusable Converter Safely (Priority: P1)

**Goal**: Give extension developers a documented, lifetime-safe registration and invocation contract.

**Independent Test**: Register converters with different lifetimes and descriptors, resolve them across execution scopes, and discover compatible descriptors without Core changes.

### Tests

- [x] T026 [P] [US2] Add failing service-lifetime and multi-scope tests in `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/OutputConverterRegistrationTests.cs`
- [x] T027 [P] [US2] Add a deterministic reference converter fixture in `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/Fixtures/ReferenceOutputConverter.cs`

### Implementation

- [x] T028 [US2] Complete public XML documentation and overloads for converter registration and contracts in `src/modules/Elsa.Workflows.Core/Extensions/OutputConverterServiceCollectionExtensions.cs` and `src/modules/Elsa.Workflows.Core/Contracts/`
- [x] T029 [US2] Add reference registration and usage coverage in `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/OutputConverterRegistrationUsageTests.cs`
- [x] T030 [US2] Run User Story 2 registration, lifetime, and reference-converter tests

**Checkpoint**: An extension module can add a discoverable scoped converter without modifying Core or persisting its implementation type.

---

## Phase 5: User Story 3 - Reject Invalid Configurations (Priority: P2)

**Goal**: Reject static configuration errors early and preserve contextual, privacy-safe runtime failures through persistence.

**Independent Test**: Publish invalid definitions and execute registration-drift/failing-converter scenarios, then inspect faults and unchanged destinations.

### Tests

- [x] T031 [P] [US3] Add failing workflow-definition validation tests in `test/unit/Elsa.Workflows.Management.UnitTests/Handlers/Notifications/ValidateOutputConvertersTests.cs`
- [x] T032 [P] [US3] Add failing safe exception-state persistence tests in `test/unit/Elsa.Workflows.Core.UnitTests/OutputConverters/OutputConversionExceptionStateTests.cs`
- [x] T033 [P] [US3] Add failing runtime drift and incident component scenarios in `test/component/Elsa.Workflows.ComponentTests/Scenarios/OutputConverters/OutputConverterFaultTests.cs`

### Implementation

- [x] T034 [US3] Implement graph-based definition validation in `src/modules/Elsa.Workflows.Management/Handlers/Notifications/ValidateOutputConverters.cs`
- [x] T035 [US3] Register definition validation in both `src/modules/Elsa.Workflows.Management/Features/WorkflowManagementFeature.cs` and `src/modules/Elsa.Workflows.Management/ShellFeatures/WorkflowManagementFeature.cs`
- [x] T036 [US3] Persist safe structured conversion metadata in `src/modules/Elsa.Workflows.Core/State/ExceptionState.cs` and populate it from the existing exception-state mapper
- [x] T037 [US3] Mirror safe exception metadata in `src/clients/Elsa.Api.Client/Resources/WorkflowInstances/Models/ExceptionState.cs`
- [x] T038 [US3] Run User Story 3 Management, Core, and component tests

**Checkpoint**: Static invalid definitions are rejected, deployment drift faults normally, and structured safe metadata survives persistence/API mapping.

---

## Phase 6: User Story 4 - Discover and Configure Converters in Studio (Priority: P2)

**Goal**: Provide server-owned discovery and complete Studio authoring/round-tripping.

**Independent Test**: Select a destination and converter in Studio, edit settings, save/reopen/clear, and verify compatibility, validation, and version-skew states.

### API and client tests

- [x] T039 [P] [US4] Add failing descriptor endpoint behavior and redaction tests in `test/unit/Elsa.Workflows.Api.UnitTests/OutputConverters/OutputConverterEndpointTests.cs`
- [x] T040 [P] [US4] Add failing API-client registration/serialization coverage in `test/component/Elsa.Workflows.ComponentTests/Scenarios/OutputConverters/OutputConverterApiClientTests.cs`

### API and client implementation

- [x] T041 [P] [US4] Add output-converter request, response, model, and Refit contract files under `src/clients/Elsa.Api.Client/Resources/OutputConverters/`
- [x] T042 [US4] Register `IOutputConvertersApi` in `src/clients/Elsa.Api.Client/Extensions/DependencyInjectionExtensions.cs`
- [x] T043 [US4] Implement authorized compatible descriptor listing under `src/modules/Elsa.Workflows.Api/Endpoints/OutputConverters/List/`
- [x] T044 [US4] Run Output Converter API and client tests

### Studio tests

- [x] T045 [P] [US4] Add failing remote-service forwarding tests in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows.Tests/OutputConverters/RemoteOutputConverterServiceTests.cs`
- [x] T046 [P] [US4] Add failing Outputs-tab discovery, selection, preservation, clear, read-only, and version-skew tests in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows.Tests/OutputConverters/OutputsTabConverterTests.cs`
- [x] T047 [P] [US4] Add failing schema/raw settings editor tests in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows.Tests/OutputConverters/OutputConverterSettingsEditorTests.cs`

### Studio implementation

- [x] T048 [P] [US4] Add `IOutputConverterService` and `RemoteOutputConverterService` under `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows.Core/Domain/Contracts/` and `Domain/Services/`
- [x] T049 [US4] Register the remote service in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows.Core/Extensions/ServiceCollectionExtensions.cs`
- [x] T050 [P] [US4] Enrich binding target metadata in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/ActivityProperties/Tabs/Outputs/Models/`
- [x] T051 [P] [US4] Implement schema-driven and raw JSON settings editing in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/ActivityProperties/Tabs/Outputs/Components/OutputConverterSettingsEditor.razor`
- [x] T052 [US4] Extend Outputs tab discovery, filtering, preservation, clearing, cancellation, read-only, and validation behavior in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows/Components/WorkflowDefinitionEditor/Components/ActivityProperties/Tabs/Outputs/Components/OutputsTab.razor` and `.razor.cs`
- [x] T053 [US4] Add localized Studio labels and errors in the existing localization resource mechanism
- [x] T054 [US4] Run Elsa Studio Workflows tests in `/Users/sipke/Projects/Elsa/elsa-studio/src/modules/Elsa.Studio.Workflows.Tests/Elsa.Studio.Workflows.Tests.csproj`

**Checkpoint**: Studio authors can discover, select, configure, save, reopen, validate, and clear converters without losing data against current or older servers.

---

## Phase 7: Polish and Cross-Cutting Verification

**Purpose**: Prove backward compatibility, privacy, performance, documentation, and cross-repository integration.

- [x] T055 [P] Add no-converter assignment benchmark coverage in `test/performance/Elsa.Workflows.PerformanceTests/OutputAssignmentBenchmark.cs`
- [x] T056 [P] Add version-skew and synthetic/workflow-as-activity coverage in `test/component/Elsa.Workflows.ComponentTests/Scenarios/OutputConverters/`
- [x] T057 [P] Update public feature documentation and samples in `doc/` and `specs/012-output-converters/quickstart.md`
- [x] T058 Run targeted Core, Management, API, client, component, and Studio test projects
- [x] T059 Run `dotnet build Elsa.sln` and the affected Studio solution/project build
- [x] T060 Run broader `./build.sh Test` or document any pre-existing/unrelated failures with evidence
- [x] T061 Audit FR-001 through FR-042 and SC-001 through SC-008 against implementation and test evidence in `specs/012-output-converters/checklists/completion.md`

---

## Dependencies and Execution Order

### Phase dependencies

- Setup precedes foundational contracts.
- Foundational contracts block every user story.
- User Stories 1 and 2 can proceed in parallel after foundations, but User Story 1's end-to-end tests use the registration contract completed in User Story 2.
- User Story 3 depends on runtime invocation and registry behavior from User Stories 1 and 2.
- User Story 4 API work depends on the descriptor registry; Studio work depends on API-client models.
- Polish and completion audit depend on all four stories.

### Parallel opportunities

- Foundational model/interface/error tasks can proceed in parallel before registry integration.
- Runtime tests, destination-resolution tests, and component tests can be authored in parallel.
- API/client and Studio component test scaffolding can proceed in parallel after contracts stabilize.
- Core and Studio builds can run independently before the cross-repository completion audit.

## Implementation Strategy

### MVP

Complete Setup, Foundational, User Story 1, and the registration portion of User Story 2. Validate converted destination values, native output preservation, atomic failure, scoped resolution, and the unchanged no-converter path.

### Incremental delivery

1. Contracts and persistence.
2. Runtime conversion and extension registration.
3. Definition validation and durable faults.
4. Descriptor API/client and Studio authoring.
5. Cross-cutting hardening and completion audit.

## Notes

- Tests must fail before their implementation tasks are completed.
- Mark each completed task `[x]` immediately.
- Do not edit or discard unrelated changes in the Studio repository.
- Do not add converter behavior to activity inputs or general expression evaluation.
