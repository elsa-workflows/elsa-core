# Feature Specification: State Machine Designer

**Feature Branch**: `005-state-machine-designer`  
**Created**: 2026-05-16  
**Status**: Draft  
**Input**: Studio UI support for elsa-core issue #5085 / PR #7457 State Machine Activity; keep Studio/X6 work separate from backend changes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View and Navigate State Machines (Priority: P1)

A workflow designer can select a `StateMachine` activity and see a dedicated state-machine canvas where states appear as nodes and directed transitions appear as route cards, without falling back to the generic property editor or reusing the full-screen Flowchart canvas.

**Why this priority**: A visual read-only graph is the minimum useful Studio support for understanding backend state-machine definitions and validating the activity contract.

**Independent Test**: Open a workflow containing a `StateMachine` with multiple states and transitions, select the activity, and verify the designer renders state nodes, transition route cards, terminal states, and graph navigation controls without changing the root Flowchart designer behavior.

**Acceptance Scenarios**:

1. **Given** a workflow root Flowchart contains a `StateMachine`, **When** the user selects or opens the `StateMachine`, **Then** Studio displays the State Machine designer instead of the generic fallback designer.
2. **Given** a `StateMachine` has states `New`, `Paid`, and `Closed`, **When** the designer loads, **Then** each state is represented once and each transition route identifies its source and target state.
3. **Given** a state has no valid outbound transitions, **When** the graph renders, **Then** the state is visually identifiable as terminal.
4. **Given** the user is editing the root Flowchart full-screen, **When** they interact with a nested State Machine designer, **Then** root Flowchart drag/drop, selection, zoom, and save behavior remains unchanged.

---

### User Story 2 - Edit States and Transitions (Priority: P2)

A workflow designer can add, rename, remove, and connect states and transitions while Studio keeps the serialized `states`, `transitions`, `initialState`, and `currentState` fields consistent with the backend StateMachine activity contract.

**Why this priority**: State-machine diagrams are only practical if designers can modify the graph shape directly instead of editing JSON-like collections by hand.

**Independent Test**: Start from an empty or small StateMachine, add states and transitions, rename a state, mark the initial state, save, reload, and verify the graph and activity JSON remain equivalent.

**Acceptance Scenarios**:

1. **Given** a StateMachine has no states, **When** the user adds the first state, **Then** Studio creates a named state and offers it as the initial state.
2. **Given** a transition is created between two states, **When** the workflow is saved, **Then** the transition stores `from` and `to` values matching the source and target state names.
3. **Given** a state is renamed, **When** the change is applied, **Then** transitions that reference the old state name are updated or the user is prevented from creating broken references.
4. **Given** the user deletes a state, **When** transitions still reference it, **Then** Studio requires a clear resolution before saving.

---

### User Story 3 - Configure Transition Slots (Priority: P3)

A workflow designer can select a transition and edit its trigger, condition, and action slots through Studio controls that match Elsa activity and expression editing conventions.

**Why this priority**: The backend activity behavior depends on transition trigger, condition, and action slots; these must be discoverable and editable without requiring raw JSON edits.

**Independent Test**: Select a transition, add or replace a trigger activity, edit a boolean condition, configure an action activity, save, reload, and verify each slot remains attached to the same transition.

**Acceptance Scenarios**:

1. **Given** a transition is selected, **When** the details panel opens, **Then** the user can edit the transition name, display name, trigger, condition, and action.
2. **Given** a trigger slot is empty, **When** the user chooses an activity, **Then** Studio embeds that child activity in the transition trigger slot.
3. **Given** a condition is edited, **When** the workflow is saved, **Then** Studio preserves the condition as an Elsa boolean input compatible with the backend activity.
4. **Given** an action slot contains an activity, **When** the user replaces or clears it, **Then** only that transition's action slot changes.

---

### User Story 4 - Edit State Entry and Exit Slots (Priority: P4)

A workflow designer can select a state and edit entry and exit child activity slots from within the State Machine designer without opening the root Flowchart designer as a nested full-screen experience.

**Why this priority**: State entry and exit are part of the state-machine model, but the first usable implementation can ship after graph and transition-slot support.

**Independent Test**: Select a state, configure entry and exit child activities, save, reload, and verify the nested activities stay attached to that state.

**Acceptance Scenarios**:

1. **Given** a state is selected, **When** the details panel opens, **Then** entry and exit slots are available as embedded child activity designers.
2. **Given** entry or exit contains an activity with properties, **When** the user edits those properties, **Then** Studio updates only the selected state's slot.
3. **Given** the nested activity is itself a Flowchart, **When** the user opens it, **Then** Studio uses an embedded or drill-in experience that does not disrupt the root Flowchart's full-screen behavior.

### Edge Cases

- State names are empty, duplicated, or contain long text.
- Transitions reference missing source or target states from older or manually edited workflow definitions.
- A StateMachine has no states, no initial state, or a current state that is no longer present.
- A transition has no trigger, no condition, no action, or a condition from an expression provider unavailable in the current Studio backend.
- Nested slot activity descriptors are unknown to the current backend activity registry.
- Large graphs contain enough states and transitions to require zoom-to-fit, pan, and readable labels.
- The backend branch is absent or the activity descriptor for `Elsa.StateMachine` is not advertised.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Studio MUST provide a dedicated diagram designer for backend activity type `Elsa.StateMachine`.
- **FR-002**: The designer MUST map `states` to state nodes and `transitions` to directed route cards using `from` and `to` state names.
- **FR-003**: The designer MUST preserve `initialState`, `currentState`, state order, transition order, and nested activity slot payloads when graph layout changes.
- **FR-004**: Users MUST be able to inspect state names, terminal status, and transition labels directly from the graph.
- **FR-005**: Users MUST be able to add, rename, and remove states with validation that prevents unresolved transition references at save time.
- **FR-006**: Users MUST be able to add, reconnect, rename, and remove transitions with validation for valid source and target state names.
- **FR-007**: Users MUST be able to edit transition `trigger`, `condition`, and `action` slots from transition selection details.
- **FR-008**: Users MUST be able to edit state `entry` and `exit` slots from state selection details.
- **FR-009**: Embedded child activity designers MUST reuse existing Studio activity picker, activity property, expression, and nested designer conventions.
- **FR-010**: State Machine designer interactions MUST NOT change full-screen root Flowchart behavior, existing Flowchart designer selection, drag/drop, zoom, auto-layout, save, or read-only behavior.
- **FR-011**: Read-only workflow definition and workflow instance views MUST render the graph and nested slot details without allowing mutation.
- **FR-012**: When the backend does not advertise the StateMachine activity descriptor, Studio MUST hide creation affordances or show an unavailable state instead of throwing.
- **FR-013**: The designer MUST display invalid or partial graphs with clear validation feedback rather than dropping unknown states, transitions, or slot payloads.
- **FR-014**: Graph navigation MUST include at least zoom-to-fit and center controls consistent with existing diagram toolbars.
- **FR-015**: The implementation MUST keep Studio-side designer code separate from elsa-core PR #7457 and avoid backend activity changes.

### Key Entities *(include if feature involves data)*

- **StateMachine Activity**: Studio JSON object for the backend activity, including `states`, `transitions`, `initialState`, and `currentState`.
- **State Node**: Visual representation of one state declaration with name, terminal marker, entry slot, and exit slot.
- **Transition Route**: Visual representation of one transition declaration with name, display name, source, target, trigger slot, condition input, and action slot.
- **Slot Activity**: Child Elsa activity embedded in a state entry/exit slot or transition trigger/action slot.
- **Validation Issue**: User-facing problem detected in the graph or serialized activity contract.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A workflow containing a three-state, two-transition StateMachine can be opened, visually inspected, saved, reloaded, and rendered with no serialized state or transition loss.
- **SC-002**: A designer can create a basic StateMachine with two states and one transition from the Studio UI in under 3 minutes without editing raw JSON.
- **SC-003**: Reopening a saved StateMachine preserves all transition trigger, condition, and action slot payloads in the same transition.
- **SC-004**: Root Flowchart designer smoke checks for selection, drag/drop, zoom-to-fit, and save continue to pass after the State Machine designer is registered.
- **SC-005**: Invalid references such as a transition targeting a missing state are visible to the user before save and are not silently discarded.

## Assumptions

- The backend StateMachine activity is supplied by elsa-core PR #7457 and is advertised to Studio as activity type `Elsa.StateMachine`.
- Studio can treat the StateMachine payload as activity JSON using existing Elsa serialization conventions.
- The first implementation may render and edit one StateMachine at a time inside the existing workflow definition editor.
- Existing X6/ReactFlow designer infrastructure can be reused, but StateMachine support should have its own mapper and wrapper so Flowchart behavior remains isolated.
- Automated component testing may be limited; lightweight validation can start with targeted builds and mapper/service tests, with sample-host manual verification documented in quickstart.
