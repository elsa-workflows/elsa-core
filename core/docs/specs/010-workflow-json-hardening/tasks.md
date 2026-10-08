# Tasks: Workflow JSON Type Hardening

**Input**: Design documents from `/specs/010-workflow-json-hardening/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Required by the feature specification for compatibility, rejection behavior, incident strategies, runtime payloads, JSON islands, and custom workflow types.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the clean worktree and documentation context.

- [x] T001 Verify the dedicated worktree status in `/Users/sipke/Projects/Elsa/elsa-core-7541`
- [x] T002 Update Spec Kit agent context in `/Users/sipke/Projects/Elsa/elsa-core-7541/AGENTS.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the dedicated workflow JSON trust boundary before changing user-facing behavior.

- [x] T003 Add serialization type registry contracts in `src/modules/Elsa.Common/Serialization/ISerializationTypeRegistry.cs`
- [x] T004 Add serialization type options in `src/modules/Elsa.Common/Serialization/SerializationTypeOptions.cs`
- [x] T005 Add serialization type registry implementation in `src/modules/Elsa.Common/Serialization/SerializationTypeRegistry.cs`
- [x] T006 Add workflow JSON registration extensions in `src/modules/Elsa.Common/Extensions/SerializationTypeOptionsExtensions.cs`
- [x] T007 Update workflow JSON resolver to use the dedicated registry in `src/modules/Elsa.Common/Serialization/SerializationTypeResolver.cs`
- [x] T008 Update workflow type converters to use the dedicated registry in `src/modules/Elsa.Workflows.Core/Serialization/Converters/TypeJsonConverter.cs`, `src/modules/Elsa.Workflows.Core/Serialization/Converters/PolymorphicObjectConverter.cs`, `src/modules/Elsa.Workflows.Core/Serialization/Converters/PolymorphicObjectConverterFactory.cs`, and `src/modules/Elsa.Workflows.Core/Serialization/Converters/PolymorphicDictionaryConverter.cs`

**Checkpoint**: Workflow JSON converters no longer depend on `ExpressionOptions`.

---

## Phase 3: User Story 1 - Load Existing Workflows Safely (Priority: P1) MVP

**Goal**: Existing registered CLR type names remain readable while unsafe names fail.

**Independent Test**: Deserialize legacy workflow JSON/type payloads with registered names and verify unsafe identifiers are rejected.

### Tests for User Story 1

- [x] T009 [US1] Update resolver tests for dedicated registry compatibility in `test/unit/Elsa.Workflows.Core.UnitTests/Serialization/Converters/SerializationTypeResolverTests.cs`
- [x] T010 [US1] Add regression coverage for expression-only aliases not being accepted by workflow JSON in `test/unit/Elsa.Workflows.Core.UnitTests/Serialization/Converters/SerializationTypeResolverTests.cs`

### Implementation for User Story 1

- [x] T011 [US1] Register core workflow JSON aliases and legacy names in `src/modules/Elsa.Workflows.Core/Features/WorkflowsFeature.cs` and `src/modules/Elsa.Workflows.Core/ShellFeatures/WorkflowsFeature.cs`
- [x] T012 [US1] Update workflow serializers and hashing to consume `ISerializationTypeRegistry` in `src/modules/Elsa.Workflows.Core/Serialization/Serializers/JsonWorkflowStateSerializer.cs`, `src/modules/Elsa.Workflows.Core/Serialization/Serializers/SafeSerializer.cs`, `src/modules/Elsa.Workflows.Core/Serialization/Serializers/BookmarkPayloadSerializer.cs`, and `src/modules/Elsa.Workflows.Core/Services/Hasher.cs`

**Checkpoint**: User Story 1 can be validated independently.

---

## Phase 4: User Story 2 - Use Consistent Type Identifiers in APIs (Priority: P2)

**Goal**: Incident strategy descriptors emit the same workflow type identifiers that workflow JSON accepts.

**Independent Test**: Descriptor output for incident strategies returns aliases and supported legacy identifiers remain readable.

### Tests for User Story 2

- [x] T013 [US2] Add incident strategy descriptor regression tests in `test/unit/Elsa.Workflows.Api.UnitTests/Endpoints/IncidentStrategies/ListTests.cs`

### Implementation for User Story 2

- [x] T014 [US2] Register incident strategy aliases and legacy names in `src/modules/Elsa.Workflows.Core/Features/WorkflowsFeature.cs` and `src/modules/Elsa.Workflows.Core/ShellFeatures/WorkflowsFeature.cs`
- [x] T015 [US2] Emit workflow JSON aliases from incident strategy descriptors in `src/modules/Elsa.Workflows.Api/Endpoints/IncidentStrategies/List/Endpoint.cs`
- [x] T016 [US2] Update client model documentation for identifier semantics in `src/clients/Elsa.Api.Client/Resources/IncidentStrategies/Models/IncidentStrategyDescriptor.cs`

**Checkpoint**: User Story 2 can be validated independently.

---

## Phase 5: User Story 3 - Register Workflow-Serializable Types Explicitly (Priority: P3)

**Goal**: Modules and host applications register workflow JSON types without using expression options.

**Independent Test**: Runtime and extension payloads resolve from workflow JSON registrations while expression-only aliases do not.

### Tests for User Story 3

- [x] T017 [US3] Update runtime feature tests for workflow JSON registrations in `test/unit/Elsa.Workflows.Runtime.UnitTests/Features/WorkflowRuntimeFeatureTests.cs`
- [x] T018 [US3] Update runtime trigger comparer tests for the dedicated registry in `test/unit/Elsa.Workflows.Runtime.UnitTests/Comparers/WorkflowTriggerEqualityComparerTests.cs`

### Implementation for User Story 3

- [x] T019 [US3] Move runtime workflow payload registration to workflow JSON options in `src/modules/Elsa.Workflows.Runtime/WorkflowRuntimeTypeAliasRegistrar.cs`, `src/modules/Elsa.Workflows.Runtime/Features/WorkflowRuntimeFeature.cs`, and `src/modules/Elsa.Workflows.Runtime/ShellFeatures/WorkflowRuntimeFeature.cs`
- [x] T020 [US3] Move module payload registrations to workflow JSON options in `src/modules/Elsa.Http/Features/HttpFeature.cs`, `src/modules/Elsa.Http/ShellFeatures/HttpFeature.cs`, `src/modules/Elsa.Scheduling/Features/SchedulingFeature.cs`, `src/modules/Elsa.Resilience/Features/ResilienceFeature.cs`, `src/modules/Elsa.Resilience/ShellFeatures/ResilienceFeature.cs`, `src/modules/Elsa.Alterations/Features/AlterationsFeature.cs`, `src/modules/Elsa.Persistence.EFCore/Modules/Management/WorkflowDefinitionPersistenceFeature.cs`, and `src/modules/Elsa.Workflows.Management/Features/WorkflowManagementFeature.cs`
- [x] T021 [US3] Update runtime trigger comparison to use `ISerializationTypeRegistry` in `src/modules/Elsa.Workflows.Runtime/Comparers/WorkflowTriggerEqualityComparer.cs` and `src/modules/Elsa.Workflows.Runtime/Services/TriggerIndexer.cs`

**Checkpoint**: User Story 3 can be validated independently.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation and validation.

- [x] T022 Add workflow JSON hardening documentation in `doc/wiki/workflow-core.md`
- [x] T023 Run targeted workflow core unit tests with `dotnet test test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj --filter SerializationTypeResolverTests`
- [x] T024 Run targeted workflow runtime unit tests with `dotnet test test/unit/Elsa.Workflows.Runtime.UnitTests/Elsa.Workflows.Runtime.UnitTests.csproj --filter WorkflowRuntimeFeatureTests`
- [x] T025 Review changed files and ensure `.specify/feature.json` and Spec Kit artifacts are correct

---

## Dependencies & Execution Order

- **Setup**: T001-T002 first.
- **Foundational**: T003-T008 block all user stories.
- **US1**: T009-T012 produces the MVP compatibility slice.
- **US2**: T013-T016 depends on the registry from Foundational and may run after US1.
- **US3**: T017-T021 depends on the registry from Foundational and may run after US1.
- **Polish**: T022-T025 after implementation.

## Parallel Opportunities

- Test updates in T009, T013, T017, and T018 can be developed in parallel after T003-T008.
- Module registration moves in T020 can be parallelized by module after the extension API exists.
- Documentation T022 can run after the contract behavior is finalized.

## Implementation Strategy

1. Establish the dedicated registry and switch converters.
2. Preserve legacy compatibility for existing workflow JSON.
3. Align incident strategy API descriptors with the registry.
4. Move runtime/module payload registrations off expression options.
5. Validate with targeted tests and documentation.
