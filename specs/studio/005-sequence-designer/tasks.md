# Tasks: Sequence Designer

**Input**: Design documents from `specs/005-sequence-designer/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Include focused tests for Sequence mapping/order behavior and build-level verification because this feature changes designer persistence semantics.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the Sequence designer structure and test project.

- [x] T001 Create `src/modules/Elsa.Studio.Workflows/DiagramDesigners/Sequences/` for the Sequence provider, designer, and wrapper classes.
- [x] T002 Create `src/modules/Elsa.Studio.Workflows.Designer/Contracts/ISequenceMapper.cs` for Sequence-to-graph mapping operations.
- [x] T003 Create `src/modules/Elsa.Studio.Workflows.Designer/Services/SequenceMapper.cs` with placeholder implementation and constructor dependencies matching existing mapper patterns.
- [x] T004 Update `src/modules/Elsa.Studio.Workflows.Designer/Contracts/IMapperFactory.cs` and `src/modules/Elsa.Studio.Workflows.Designer/Services/MapperFactory.cs` to expose `CreateSequenceMapperAsync`.
- [x] T005 [P] Add `src/modules/Elsa.Studio.Workflows.Designer.Tests/Elsa.Studio.Workflows.Designer.Tests.csproj` and include it in `Elsa.Studio.sln`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared React Flow mode and registration hooks required by all user stories.

**CRITICAL**: No user story work can begin until this phase is complete.

- [x] T006 Register `SequenceMapper` in `src/modules/Elsa.Studio.Workflows.Designer/Extensions/ServiceCollectionExtensions.cs`.
- [x] T007 Add `SequenceDiagramDesignerProvider` in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/Sequences/SequenceDiagramDesignerProvider.cs` that supports `Elsa.Sequence`.
- [x] T008 Register `SequenceDiagramDesignerProvider` in `src/modules/Elsa.Studio.Workflows/Extensions/ServiceCollectionExtensions.cs`.
- [x] T009 Add React designer mode and Sequence layout orientation values to `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer/types.ts`.
- [x] T010 Update `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer/components/Designer.tsx` to accept and store `flowchart` and `sequence` modes without changing existing Flowchart behavior.
- [x] T011 Update `src/modules/Elsa.Studio.Workflows.Designer/Interop/ReactFlowGraphApi.cs` and `src/modules/Elsa.Studio.Workflows.Designer/Interop/ReactFlowJsInterop.cs` to pass the selected designer mode and Sequence orientation during graph creation.

**Checkpoint**: Sequence provider can be registered, and React Flow can distinguish constrained Sequence mode from existing Flowchart mode.

---

## Phase 3: User Story 1 - View and edit a Sequence visually (Priority: P1) MVP

**Goal**: Users can open a Sequence, view children in order, add/insert activities, save, and reload with the same order.

**Independent Test**: Open a Sequence with multiple children, insert a new child between two existing children, save, reload, and verify order is preserved.

### Tests for User Story 1

- [x] T012 [P] [US1] Add Sequence mapping tests for ordered child-to-node/derived-edge output in `src/modules/Elsa.Studio.Workflows.Designer.Tests/SequenceMapperTests.cs`.
- [x] T013 [P] [US1] Add Sequence readback tests proving derived edges are discarded and child order is persisted in `src/modules/Elsa.Studio.Workflows.Designer.Tests/SequenceMapperTests.cs`.
- [x] T014 [P] [US1] Add Sequence orientation metadata tests proving vertical default and saved horizontal orientation in `src/modules/Elsa.Studio.Workflows.Designer.Tests/SequenceMapperTests.cs`.

### Implementation for User Story 1

- [x] T015 [US1] Implement `SequenceMapper.Map` from Sequence child collection to nodes plus derived structural edges in `src/modules/Elsa.Studio.Workflows.Designer/Services/SequenceMapper.cs`.
- [x] T016 [US1] Implement `SequenceMapper.Map` readback from visual node order to updated Sequence child collection in `src/modules/Elsa.Studio.Workflows.Designer/Services/SequenceMapper.cs`.
- [x] T017 [US1] Add `SequenceDesignerWrapper.razor` and `SequenceDesignerWrapper.razor.cs` in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/Sequences/` to mirror Flowchart wrapper callbacks and read/write lifecycle.
- [x] T018 [US1] Add `SequenceDiagramDesigner.cs` in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/Sequences/` implementing `IDiagramDesignerToolboxProvider`.
- [x] T019 [US1] Add `SequenceFlowDesigner.razor` and `SequenceFlowDesigner.razor.cs` in `src/modules/Elsa.Studio.Workflows.Designer/Components/` to load/read Sequence graphs through `ISequenceMapper`.
- [x] T020 [US1] Implement empty Sequence add/drop target, insert-between affordances, and orientation switching in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer/components/Designer.tsx`.
- [x] T021 [US1] Add Sequence-mode styles for ordered layout and insertion affordances in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer.css`.
- [ ] T022 [US1] Add distinct empty, loading, unavailable, validation-failed, and save-failed states for Sequence mode in `src/modules/Elsa.Studio.Workflows.Designer/Components/SequenceFlowDesigner.razor.cs`.
- [x] T023 [US1] Wire selected, invalid, faulted, running, completed, and disabled visual states for Sequence child nodes in `src/modules/Elsa.Studio.Workflows.Designer/Services/SequenceMapper.cs`.
- [x] T024 [US1] Build the designer client bundle from `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/package.json`.

**Checkpoint**: User Story 1 is functional and testable independently.

---

## Phase 4: User Story 2 - Reorder Sequence children safely (Priority: P2)

**Goal**: Users can reorder Sequence children visually while invalid free-form graph structures are blocked.

**Independent Test**: Reorder several Sequence children, save, reload, and verify execution order and visual order match with no persisted arbitrary links.

### Tests for User Story 2

- [x] T025 [P] [US2] Add Sequence reorder tests preserving activity IDs/configuration in `src/modules/Elsa.Studio.Workflows.Designer.Tests/SequenceMapperTests.cs`.
- [x] T026 [P] [US2] Add Sequence invalid-edge readback tests in `src/modules/Elsa.Studio.Workflows.Designer.Tests/SequenceMapperTests.cs`.

### Implementation for User Story 2

- [x] T027 [US2] Implement Sequence-mode drag reorder and explicit move command handling in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer/components/Designer.tsx`.
- [x] T028 [US2] Disable manual connection creation in Sequence mode in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer/components/Designer.tsx`.
- [x] T029 [US2] Ensure Sequence-mode copy, paste, duplicate, and delete update ordered children in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer/components/Designer.tsx`.
- [x] T030 [US2] Update `src/modules/Elsa.Studio.Workflows.Designer/Components/SequenceFlowDesigner.razor.cs` to preserve activity configuration and nested bodies during reorder/readback.
- [x] T031 [US2] Add move/auto-layout toolbox behavior for Sequence in `src/modules/Elsa.Studio.Workflows/DiagramDesigners/Sequences/SequenceDiagramDesigner.cs`.

**Checkpoint**: User Stories 1 and 2 work independently.

---

## Phase 5: User Story 3 - Work with embedded outcomes and bodies (Priority: P3)

**Goal**: Users can see and drill into embedded bodies/outcomes from Sequence children without expanding all nested content inline.

**Independent Test**: Add an activity with an embedded body or named outcome inside a Sequence, open that region, edit it, navigate back, and confirm the parent Sequence remains intact.

### Tests for User Story 3

- [x] T032 [P] [US3] Add embedded region mapping tests for Sequence children in `src/modules/Elsa.Studio.Workflows.Designer.Tests/SequenceMapperTests.cs`.

### Implementation for User Story 3

- [x] T033 [US3] Render Sequence child embedded ports/outcomes with existing activity node affordances in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer/components/ActivityNode.tsx`.
- [x] T034 [US3] Route Sequence embedded-port clicks through existing `ActivityEmbeddedPortSelected` callbacks in `src/modules/Elsa.Studio.Workflows.Designer/Components/SequenceFlowDesigner.razor.cs`.
- [ ] T035 [US3] Verify and adjust breadcrumb/drill-in behavior for Sequence scopes in `src/modules/Elsa.Studio.Workflows/Shared/Components/DiagramDesignerWrapper.razor.cs`.
- [ ] T036 [US3] Add empty embedded-region visual state in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib/src/react-designer/components/ActivityNode.tsx`.

**Checkpoint**: All user stories are independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verification, performance, and cleanup across the designer.

- [x] T037 [P] Run `dotnet test Elsa.Studio.sln` from the repository root and fix Sequence designer regressions.
- [x] T038 [P] Run `npm run build` in `src/modules/Elsa.Studio.Workflows.Designer/ClientLib` and fix TypeScript/bundle regressions.
- [ ] T039 Manually verify `specs/005-sequence-designer/quickstart.md` against a Studio host.
- [ ] T040 Verify existing Flowchart behavior with React Flow enabled and X6 fallback disabled/enabled as applicable.
- [ ] T041 Verify a Sequence with at least 100 children remains readable and interactive in vertical and horizontal orientations.
- [x] T042 Update developer notes or XML/package descriptions that still imply the workflows designer is X6-only in `src/modules/Elsa.Studio.Workflows.Designer/Elsa.Studio.Workflows.Designer.csproj`.
- [x] T043 Review repeated Sequence/Flowchart wrapper logic and extract shared helpers only where duplication is structural.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Phase 1 and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Phase 2 and forms the MVP.
- **User Story 2 (Phase 4)**: Depends on Sequence rendering/readback from US1.
- **User Story 3 (Phase 5)**: Depends on Sequence rendering from US1 and can proceed partly in parallel with US2 after T019.
- **Polish (Phase 6)**: Depends on all desired user stories.

### User Story Dependencies

- **US1**: Required MVP.
- **US2**: Requires US1 mapper/component foundation.
- **US3**: Requires US1 rendering foundation; mostly independent from US2.

### Parallel Opportunities

- T005 can run in parallel with T001-T004.
- T009 can run in parallel with T006-T008.
- T012, T013, and T014 can be written in parallel before T015/T016.
- T025 and T026 can be written in parallel before T027/T028.
- T032 can be written in parallel with T033 after Sequence mapping shape is stable.
- T037 and T038 can run in parallel.

## Parallel Example: User Story 1

```text
Task: "T012 [US1] Add Sequence mapping tests for ordered child-to-node/derived-edge output"
Task: "T013 [US1] Add Sequence readback tests proving derived edges are discarded"
```

## Implementation Strategy

### MVP First

1. Complete Phase 1 and Phase 2.
2. Complete Phase 3 only.
3. Validate add/insert/save/reload for Sequence.
4. Demo the ordered visual designer before adding reorder and embedded-region polish.

### Incremental Delivery

1. US1: visual ordered Sequence editing.
2. US2: robust reorder and invalid graph prevention.
3. US3: embedded outcome/body previews and drill-in.
4. Polish: large workflow, read-only, Flowchart regression, X6 fallback verification.
