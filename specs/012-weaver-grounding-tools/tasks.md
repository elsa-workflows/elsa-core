# Tasks: Weaver Grounding Tools

**Input**: Design documents from `specs/012-weaver-grounding-tools/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: New grounding code must include unit and integration coverage.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish shared grounding DTOs, options, and registration surfaces.

- [X] T001 Add grounding DTO records in `src/modules/Elsa.AI.Abstractions/Models/AIGroundingModels.cs`.
- [X] T002 Add grounding result size and paging options in `src/modules/Elsa.AI.Host/Options/AIHostOptions.cs`.
- [X] T003 [P] Add grounding tool registration extension helpers in `src/modules/Elsa.AI.Host/Features/AIFeature.cs`.
- [X] T004 [P] Update AI capability response models in `src/modules/Elsa.AI.Host/Endpoints/AI/Capabilities/Endpoint.cs`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared services that all grounding tool families need.

- [X] T005 Add redaction and size-clamping helpers for grounding payloads in `src/modules/Elsa.AI.Host/Services/AIGroundingResultFormatter.cs`.
- [X] T006 Add model-safe activity descriptor mapping service in `src/modules/Elsa.AI.Host/Services/ActivityGroundingMapper.cs`.
- [X] T007 Add model-safe workflow graph mapping service in `src/modules/Elsa.AI.Host/Services/WorkflowGroundingMapper.cs`.
- [X] T008 Add model-safe runtime instance mapping service in `src/modules/Elsa.AI.Host/Services/RuntimeGroundingMapper.cs`.
- [X] T009 Register grounding services and built-in tools in `src/modules/Elsa.AI.Host/Extensions/ServiceCollectionExtensions.cs`.
- [X] T010 [P] Add unit tests for result formatting and redaction in `test/unit/Elsa.AI.Host.UnitTests/Grounding/AIGroundingResultFormatterTests.cs`.
- [X] T011 [P] Add unit tests for capability descriptor composition in `test/unit/Elsa.AI.Host.UnitTests/Grounding/AIGroundingCapabilityTests.cs`.

**Checkpoint**: Shared grounding services are available and tested.

---

## Phase 3: User Story 1 - Discover available activities (Priority: P1) MVP

**Goal**: Let Weaver search and inspect installed Activity Registry metadata.

**Independent Test**: Search installed activities by capability and get one descriptor without using workflow or runtime tools.

### Tests for User Story 1

- [X] T012 [P] [US1] Add unit tests for activity descriptor mapping in `test/unit/Elsa.AI.Host.UnitTests/Grounding/ActivityGroundingMapperTests.cs`.
- [X] T013 [P] [US1] Add integration tests for `activities.search` and `activities.getDescriptor` in `test/integration/Elsa.AI.IntegrationTests/AIActivityGroundingToolTests.cs`.

### Implementation for User Story 1

- [X] T014 [US1] Implement `activities.search` tool in `src/modules/Elsa.AI.Host/Tools/Activities/ActivitiesSearchTool.cs`.
- [X] T015 [US1] Implement `activities.getDescriptor` tool in `src/modules/Elsa.AI.Host/Tools/Activities/ActivityDescriptorTool.cs`.
- [X] T016 [US1] Add activity search filtering by query, category, type, version, input, output, and trigger behavior in `src/modules/Elsa.AI.Host/Services/ActivityGroundingSearchService.cs`.
- [X] T017 [US1] Expose activity grounding capability metadata in `src/modules/Elsa.AI.Host/Endpoints/AI/Capabilities/Endpoint.cs`.
- [X] T018 [US1] Verify activity tools are listed by `GET /ai/tools` in `test/integration/Elsa.AI.IntegrationTests/AIToolsEndpointTests.cs`.

**Checkpoint**: Weaver can ground authoring in installed activities.

---

## Phase 4: User Story 2 - Understand workflow definitions (Priority: P2)

**Goal**: Let Weaver search, retrieve, explain, and compare authorized workflow definitions.

**Independent Test**: Attach or search a workflow definition and ask for a graph summary without creating a proposal.

### Tests for User Story 2

- [X] T019 [P] [US2] Add unit tests for workflow graph mapping in `test/unit/Elsa.AI.Host.UnitTests/Grounding/WorkflowGroundingMapperTests.cs`.
- [X] T020 [P] [US2] Add integration tests for workflow search/detail tools in `test/integration/Elsa.AI.IntegrationTests/AIWorkflowGroundingToolTests.cs`.

### Implementation for User Story 2

- [X] T021 [US2] Implement `workflows.search` tool in `src/modules/Elsa.AI.Host/Tools/Workflows/WorkflowsSearchTool.cs`.
- [X] T022 [US2] Implement `workflows.getDefinition` tool in `src/modules/Elsa.AI.Host/Tools/Workflows/WorkflowDefinitionTool.cs`.
- [X] T023 [US2] Implement `workflows.getDefinitionGraph` tool in `src/modules/Elsa.AI.Host/Tools/Workflows/WorkflowDefinitionGraphTool.cs`.
- [X] T024 [US2] Implement `workflows.findUsages` tool in `src/modules/Elsa.AI.Host/Tools/Workflows/WorkflowUsageSearchTool.cs`.
- [X] T025 [US2] Extend `WorkflowDefinitionContextProvider` in `src/modules/Elsa.AI.Host/Context/WorkflowDefinitionContextProvider.cs` to use model-safe graph summaries.

**Checkpoint**: Weaver can explain and search workflows using authorized definition data.

---

## Phase 5: User Story 3 - Create and update workflows safely (Priority: P3)

**Goal**: Let Weaver create or update workflow proposals using installed activity metadata and validation.

**Independent Test**: Ask Weaver to create or update a workflow and verify proposal-only output with diagnostics.

### Tests for User Story 3

- [X] T026 [P] [US3] Add unit tests for draft validation against activity descriptors in `test/unit/Elsa.AI.Host.UnitTests/Grounding/WorkflowDraftValidationTests.cs`.
- [X] T027 [P] [US3] Add integration tests for proposal tools in `test/integration/Elsa.AI.IntegrationTests/AIWorkflowProposalToolTests.cs`.

### Implementation for User Story 3

- [X] T028 [US3] Implement workflow draft validation service in `src/modules/Elsa.AI.Host/Services/WorkflowDraftValidationService.cs`.
- [X] T029 [US3] Implement `workflows.validateDraft` tool in `src/modules/Elsa.AI.Host/Tools/Workflows/WorkflowValidateDraftTool.cs`.
- [X] T030 [US3] Implement `workflows.proposeCreate` tool in `src/modules/Elsa.AI.Host/Tools/Workflows/WorkflowProposeCreateTool.cs`.
- [X] T031 [US3] Implement `workflows.proposeUpdate` tool in `src/modules/Elsa.AI.Host/Tools/Workflows/WorkflowProposeUpdateTool.cs`.
- [X] T032 [US3] Add proposal graph diff generation in `src/modules/Elsa.AI.Host/Services/WorkflowProposalDiffService.cs`.
- [X] T033 [US3] Add stale baseline checks for update proposals in `src/modules/Elsa.AI.Host/Services/WorkflowDraftValidationService.cs`.

**Checkpoint**: Weaver can produce governed workflow authoring proposals.

---

## Phase 6: User Story 4 - Inspect runtime instances and incidents (Priority: P4)

**Goal**: Let Weaver inspect workflow instances and incidents with redacted runtime evidence.

**Independent Test**: Ask Weaver why a failed seeded instance failed and verify evidence-backed output.

### Tests for User Story 4

- [X] T034 [P] [US4] Add unit tests for runtime grounding mapping in `test/unit/Elsa.AI.Host.UnitTests/Grounding/RuntimeGroundingMapperTests.cs`.
- [X] T035 [P] [US4] Add integration tests for instance and incident tools in `test/integration/Elsa.AI.IntegrationTests/AIRuntimeGroundingToolTests.cs`.

### Implementation for User Story 4

- [X] T036 [US4] Implement `instances.search` tool in `src/modules/Elsa.AI.Host/Tools/Runtime/InstancesSearchTool.cs`.
- [X] T037 [US4] Implement `instances.get` tool in `src/modules/Elsa.AI.Host/Tools/Runtime/WorkflowInstanceTool.cs`.
- [X] T038 [US4] Implement `instances.getExecutionHistory` tool in `src/modules/Elsa.AI.Host/Tools/Runtime/WorkflowInstanceExecutionHistoryTool.cs`.
- [X] T039 [US4] Implement `instances.getActivityState` tool in `src/modules/Elsa.AI.Host/Tools/Runtime/WorkflowInstanceActivityStateTool.cs`.
- [X] T040 [US4] Implement `incidents.search` tool in `src/modules/Elsa.AI.Host/Tools/Runtime/IncidentsSearchTool.cs`.
- [X] T041 [US4] Implement `incidents.get` tool in `src/modules/Elsa.AI.Host/Tools/Runtime/IncidentTool.cs`.
- [X] T042 [US4] Extend `WorkflowInstanceContextProvider` in `src/modules/Elsa.AI.Host/Context/WorkflowInstanceContextProvider.cs` to include bounded incident and activity state summaries.

**Checkpoint**: Weaver can answer runtime inspection questions from authorized instance and incident data.

---

## Phase 7: User Story 5 - Surface grounding capabilities to Studio (Priority: P5)

**Goal**: Let Studio discover and render available grounding features without provider-specific assumptions.

**Independent Test**: Call capabilities and tools endpoints and verify grounding families, attachment kinds, and unavailable states.

### Tests for User Story 5

- [X] T043 [P] [US5] Extend capability endpoint tests in `test/integration/Elsa.AI.IntegrationTests/AICapabilitiesEndpointTests.cs`.
- [X] T044 [P] [US5] Extend chat stream tests for grounded tool result events in `test/integration/Elsa.AI.IntegrationTests/AIChatEndpointTests.cs`.

### Implementation for User Story 5

- [X] T045 [US5] Add grounding capability response fields in `src/modules/Elsa.AI.Host/Endpoints/AI/Capabilities/Endpoint.cs`.
- [X] T046 [US5] Add supported attachment kinds for activity, workflow, runtime, diagnostics, and time range in `src/modules/Elsa.AI.Abstractions/Models/AIContextAttachment.cs`.
- [X] T047 [US5] Add disabled grounding capability reasons in `src/modules/Elsa.AI.Host/Endpoints/AI/Capabilities/Endpoint.cs`.

**Checkpoint**: Studio can discover Weaver grounding support and render UI affordances safely.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, safety review, and verification.

- [X] T048 [P] Add host README documentation for grounding tools in `src/modules/Elsa.AI.Host/README.md`.
- [X] T049 [P] Update Weaver quickstart references in `specs/008-weaver-ai-copilot/quickstart.md`.
- [X] T050 Run `dotnet test test/unit/Elsa.AI.Host.UnitTests/Elsa.AI.Host.UnitTests.csproj`.
- [X] T051 Run `dotnet test test/integration/Elsa.AI.IntegrationTests/Elsa.AI.IntegrationTests.csproj`.
- [X] T052 Run `dotnet build Elsa.sln -m:1`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user stories.
- **US1 Activity Discovery**: First MVP story after Foundation.
- **US2 Workflow Understanding**: Can start after Foundation; benefits from US1 for missing activity warnings.
- **US3 Workflow Proposals**: Depends on US1 and US2.
- **US4 Runtime Inspection**: Can start after Foundation and proceed independently of US3.
- **US5 Studio Capabilities**: Can start after each tool family has capability metadata.
- **Polish**: Depends on implemented target stories.

### Parallel Opportunities

- T003 and T004 can run in parallel.
- T010 and T011 can run in parallel.
- Test tasks within each user story can run in parallel with each other.
- US2 and US4 can be implemented in parallel after Foundation.

## Implementation Strategy

### MVP First

1. Complete Setup and Foundation.
2. Complete US1 activity discovery.
3. Validate that Weaver can search installed activities and answer activity questions.
4. Add US2 workflow understanding.
5. Add US3 proposal creation/update only after activity and workflow grounding are reliable.

### Incremental Delivery

1. Deliver activity grounding.
2. Deliver workflow explanation/search.
3. Deliver proposal validation and proposal creation/update.
4. Deliver runtime inspection.
5. Deliver Studio capability metadata for all enabled tool families.
