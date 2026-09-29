# Tasks: State Machine Designer

**Input**: Design documents from `/specs/005-state-machine-designer/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Include focused mapper/validator tests where test infrastructure exists or as part of the first implementation slice.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish isolated StateMachine designer services and tests without modifying Flowchart behavior.

- [X] T001 Create StateMachine graph model files in `src/modules/Elsa.Studio.Workflows.Designer/Models/StateMachineGraph.cs`, `StateMachineStateNode.cs`, `StateMachineTransitionEdge.cs`, and `StateMachineValidationIssue.cs`
- [X] T002 Create `IStateMachineMapper` contract in `src/modules/Elsa.Studio.Workflows.Designer/Contracts/IStateMachineMapper.cs`
- [X] T003 Create `StateMachineMapper` and `StateMachineValidator` service skeletons in `src/modules/Elsa.Studio.Workflows.Designer/Services/`
- [X] T004 Register StateMachine mapper/validator services in `src/modules/Elsa.Studio.Workflows.Designer/Extensions/ServiceCollectionExtensions.cs`
- [X] T005 Create or extend mapper test project for `Elsa.Studio.Workflows.Designer` under `src/modules/Elsa.Studio.Workflows.Designer.Tests/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Prove StateMachine JSON can round-trip safely before UI mutation.

- [X] T006 [P] Add mapper test for loading states and transitions from sample JSON in `src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineMapperTests.cs`
- [X] T007 [P] Add validator tests for empty state name, duplicate state name, missing endpoints, missing target, and unknown slot activity in `src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineValidatorTests.cs`
- [X] T008 Implement StateMachine JSON-to-graph mapping in `src/modules/Elsa.Studio.Workflows.Designer/Services/StateMachineMapper.cs`
- [X] T009 Implement graph-to-StateMachine JSON round-trip preserving order, nested slots, missing condition, and unknown properties in `src/modules/Elsa.Studio.Workflows.Designer/Services/StateMachineMapper.cs`
- [X] T010 Implement validation issue generation in `src/modules/Elsa.Studio.Workflows.Designer/Services/StateMachineValidator.cs`

**Checkpoint**: Mapper and validator can run without any UI changes.

---

## Phase 3: User Story 1 - View and Navigate State Machines (Priority: P1) MVP

**Goal**: Dedicated read-only StateMachine designer renders states and transitions without changing root Flowchart behavior.

**Independent Test**: Open a workflow containing a StateMachine and verify the dedicated designer renders state nodes, transition route cards, terminal markers, and navigation controls while root Flowchart still works.

### Tests for User Story 1

- [X] T011 [P] [US1] Add provider support tests for `Elsa.StateMachine` and non-StateMachine activities in `src/modules/Elsa.Studio.Workflows.Tests/StateMachineDiagramDesignerProviderTests.cs` if a Workflows test project exists or is introduced
- [X] T012 [P] [US1] Add mapper test for terminal state detection in `src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineMapperTests.cs`

### Implementation for User Story 1

- [X] T013 [US1] Create `StateMachineDiagramDesignerProvider` in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDiagramDesignerProvider.cs`
- [X] T014 [US1] Create `StateMachineDiagramDesigner` in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDiagramDesigner.cs`
- [X] T015 [US1] Create read-only `StateMachineDesignerWrapper.razor` and code-behind in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/`
- [X] T016 [US1] Register StateMachine designer provider in `src/modules/Elsa.Studio.Workflows/Extensions/ServiceCollectionExtensions.cs`
- [X] T017 [US1] Add zoom-to-fit and center toolbar actions to `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDiagramDesigner.cs`
- [X] T018 [US1] Verify root Flowchart selection, drag/drop, zoom-to-fit, auto-layout, and save behavior in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/Flowcharts/`

**Checkpoint**: User Story 1 is demonstrable as a read-only MVP.

---

## Phase 4: User Story 2 - Edit States and Transitions (Priority: P2)

**Goal**: Users can change state and transition graph shape and save a valid StateMachine payload.

**Independent Test**: Add two states and one transition, save, reload, and verify equivalent JSON and graph.

### Tests for User Story 2

- [X] T019 [P] [US2] Add round-trip test for adding states and a transition in `src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineMapperTests.cs`
- [X] T020 [P] [US2] Add rename propagation or validation test in `src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineValidatorTests.cs`

### Implementation for User Story 2

- [X] T021 [US2] Add state add/rename/delete commands to `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor.cs`
- [X] T022 [US2] Add transition add/reconnect/delete commands to `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor.cs`
- [X] T023 [US2] Implement `ReadRootActivityAsync` graph-to-JSON save path in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDiagramDesigner.cs`
- [X] T024 [US2] Add validation feedback UI for blocking state/transition errors in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor`
- [X] T025 [US2] Preserve `initialState` and `currentState` selections during graph edits in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor.cs`

**Checkpoint**: Basic graph editing is independently usable.

---

## Phase 5: User Story 3 - Configure Transition Slots (Priority: P3)

**Goal**: Users can configure transition trigger, condition, and action slots from transition details.

**Independent Test**: Add trigger, condition, and action to one transition, save, reload, and verify slots remain attached.

### Tests for User Story 3

- [X] T026 [P] [US3] Add mapper round-trip test for trigger, condition, and action slot payloads in `src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineMapperTests.cs`

### Implementation for User Story 3

- [X] T027 [US3] Add transition details panel to `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor`
- [X] T028 [US3] Integrate activity picker for transition trigger/action slots in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor.cs`
- [ ] T029 [US3] Integrate existing boolean expression input for transition condition in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor`
- [X] T030 [US3] Preserve unknown or unavailable transition slot activities as placeholders in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor`

**Checkpoint**: Transition behavior-critical slots are editable.

---

## Phase 6: User Story 4 - Edit State Entry and Exit Slots (Priority: P4)

**Goal**: Users can configure state entry and exit child activities without disrupting the root Flowchart designer.

**Independent Test**: Add entry and exit child activities to one state, save, reload, and verify slots remain attached.

### Tests for User Story 4

- [X] T031 [P] [US4] Add mapper round-trip test for state entry and exit slot payloads in `src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineMapperTests.cs`

### Implementation for User Story 4

- [X] T032 [US4] Add state details panel to `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor`
- [X] T033 [US4] Integrate activity picker for state entry/exit slots in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor.cs`
- [ ] T034 [US4] Add embedded or drill-in nested activity designer handling for state slot activities in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/StateMachines/StateMachineDesignerWrapper.razor`
- [ ] T035 [US4] Preserve root Flowchart full-screen behavior when nested state slot designers are opened in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/Flowcharts/FlowchartDesignerWrapper.razor.cs`

**Checkpoint**: State lifecycle slots are editable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verification, docs, and fit-and-finish across all stories.

- [X] T036 [P] Update quickstart verification notes in `specs/005-state-machine-designer/quickstart.md`
- [X] T037 [P] Add XML docs or comments only for non-obvious mapper/interop lifecycle constraints in `src/modules/Elsa.Studio.Workflows.Designer/Services/StateMachineMapper.cs`
- [X] T038 Run `dotnet build src/modules/Elsa.Studio.Workflows.Designer/Elsa.Studio.Workflows.Designer.csproj`
- [X] T039 Run `dotnet build src/modules/Elsa.Studio.Workflows/Elsa.Studio.Workflows.csproj`
- [X] T040 Run StateMachine mapper tests in `src/modules/Elsa.Studio.Workflows.Designer.Tests/Elsa.Studio.Workflows.Designer.Tests.csproj`
- [ ] T041 Perform sample-host manual verification from `specs/005-state-machine-designer/quickstart.md` (blocked until a backend advertising `Elsa.StateMachine` is running)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup; blocks UI work.
- **US1 MVP (Phase 3)**: Depends on Foundational mapper/validator.
- **US2 (Phase 4)**: Depends on US1.
- **US3 (Phase 5)**: Depends on Foundational and can start after transition selection exists from US1/US2.
- **US4 (Phase 6)**: Depends on Foundational and can start after state selection exists from US1/US2.
- **Polish (Phase 7)**: Depends on implemented stories.

### User Story Dependencies

- **User Story 1 (P1)**: First usable slice and MVP.
- **User Story 2 (P2)**: Builds on US1 rendering and graph selection.
- **User Story 3 (P3)**: Builds on transition selection and mapper slot preservation.
- **User Story 4 (P4)**: Builds on state selection and nested slot editing decisions.

### Parallel Opportunities

- T006 and T007 can run in parallel after Setup.
- Provider tests and terminal-state mapper tests can run in parallel for US1.
- US3 and US4 slot mapper tests can run in parallel after Foundational.
- Documentation and validation commands can run independently once code exists.

---

## Parallel Example: Foundational Mapper Work

```bash
Task: "Add mapper test for loading states and transitions in src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineMapperTests.cs"
Task: "Add validator tests in src/modules/Elsa.Studio.Workflows.Designer.Tests/StateMachineValidatorTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Setup and Foundational mapper/validator tasks.
2. Register the dedicated provider and read-only wrapper.
3. Validate StateMachine graph rendering against sample JSON.
4. Stop and verify root Flowchart behavior is unchanged.

### Incremental Delivery

1. Add graph inspection.
2. Add graph shape editing.
3. Add transition slot editing.
4. Add state entry/exit slot editing.
5. Harden validation and sample-host verification.

### First Safe Slice

The first safe implementation slice is T001-T010 plus T013-T018. It avoids changing backend code, limits Flowchart impact to provider registration, and proves read-only rendering before mutation-heavy editing.
