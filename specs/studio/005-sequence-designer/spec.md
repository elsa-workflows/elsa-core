# Feature Specification: Sequence Designer

**Feature Branch**: `005-sequence-designer`  
**Created**: 2026-05-16  
**Status**: Clarified  
**Input**: User description: "Implement a React Flow based visual designer for the Sequence activity. The designer should share the new Flowchart designer UX direction, render Sequence as a constrained ordered graph rather than a free-form flowchart, support insert-between and reorder interactions, surface outcome/body embeddings with drill-in behavior, and keep the ordered child activity collection as the source of truth."

## Clarifications

### Session 2026-05-17

- Q: What layout direction should the Sequence designer use? -> A: Support both vertical and horizontal layouts, with vertical as the default.
- Q: Should the selected Sequence layout orientation be saved? -> A: Save the layout orientation with Sequence designer metadata.
- Q: How should outcomes display inside a Sequence? -> A: Show compact inline outcome previews, but edit outcomes through drill-in.
- Q: How should manual connection gestures behave in Sequence mode? -> A: Disable manual connections entirely in Sequence mode.
- Q: Which reorder interactions should the Sequence designer support? -> A: Support both drag reorder and explicit move commands.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View and edit a Sequence visually (Priority: P1)

A workflow author opens a workflow containing a Sequence activity and sees its child activities as a clear ordered visual chain that can be edited without falling back to raw property editing.

**Why this priority**: Sequence is a core composition activity. The first useful increment must let authors understand and edit ordered execution visually.

**Independent Test**: Create or open a workflow with a Sequence containing multiple child activities, open the Sequence designer, insert a new activity between two existing activities, save, reload, and verify the order is preserved.

**Acceptance Scenarios**:

1. **Given** a Sequence with three child activities, **When** the user opens the Sequence designer, **Then** the activities are displayed in execution order from start to end.
2. **Given** the user inserts an activity between two existing Sequence children, **When** the workflow is saved and reopened, **Then** the new activity remains at the inserted position.
3. **Given** a Sequence has no child activities, **When** the designer opens, **Then** the user sees an empty-state drop target and an action to add the first activity.

---

### User Story 2 - Reorder Sequence children safely (Priority: P2)

A workflow author rearranges child activities inside a Sequence using visual interactions while the designer prevents invalid free-form graph structures.

**Why this priority**: Ordering is the defining behavior of Sequence. Reordering must be efficient and constrained so users cannot accidentally create flowchart semantics.

**Independent Test**: Open a Sequence with several activities, reorder them visually, save, reload, and verify the underlying execution order and visual order match.

**Acceptance Scenarios**:

1. **Given** a Sequence with multiple activities, **When** the user drags one activity to a different valid position, **Then** the visual chain updates and the activity collection order changes accordingly.
2. **Given** a user attempts to manually create an arbitrary connection between Sequence activities, **When** the interaction would produce a non-linear graph, **Then** the designer blocks the connection gesture and does not create or persist a link.
3. **Given** an activity is selected, **When** the user uses available move commands, **Then** the selected activity moves earlier or later in the Sequence without changing its configuration.

---

### User Story 3 - Work with embedded outcomes and bodies (Priority: P3)

A workflow author can see when a Sequence child has outcomes or nested bodies and drill into those regions without expanding the entire nested workflow inline.

**Why this priority**: Many activities inside a Sequence contain child regions or outcomes. The designer needs to expose these affordances while keeping the Sequence readable.

**Independent Test**: Add decision-like and body-containing activities inside a Sequence, verify their embedded regions are visible as previews, drill into one region, edit its contents, navigate back, and confirm the parent Sequence remains intact.

**Acceptance Scenarios**:

1. **Given** a Sequence child has named outcomes, **When** the Sequence designer renders the activity, **Then** the outcomes are visible as constrained branch affordances or previews rather than arbitrary Sequence links.
2. **Given** a Sequence child has a body or branch region, **When** the user opens that embedded region, **Then** the designer enters the child scope with breadcrumbs that allow returning to the parent Sequence.
3. **Given** a nested region is empty, **When** the parent Sequence renders the child activity, **Then** the empty region is represented clearly without occupying excessive canvas space.

### Edge Cases

- A Sequence contains zero children.
- A Sequence contains one child, where reorder controls should not imply unavailable actions.
- A Sequence contains many children and must remain scannable.
- A child activity has long display text or description text.
- A child activity has validation errors, warnings, incidents, or execution status.
- A child activity exposes multiple named outcomes.
- A child activity contains nested bodies that are themselves Sequences or Flowcharts.
- A workflow created outside Studio defines a Sequence with child activity metadata missing designer-specific layout information.
- A Sequence does not have saved layout orientation metadata.
- A user deletes the selected activity or the activity currently open in a nested scope.
- A user copies or duplicates activities that contain nested bodies.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a dedicated visual designer for Sequence activities.
- **FR-002**: The Sequence designer MUST represent Sequence children as an ordered visual chain, not as an unconstrained free-form graph.
- **FR-003**: The system MUST use the same visual language and interaction direction as the current enhanced Flowchart designer where applicable, including activity nodes, selection behavior, viewport behavior, execution/validation indicators, and drill-in navigation.
- **FR-004**: Users MUST be able to add the first activity to an empty Sequence.
- **FR-005**: Users MUST be able to insert a new activity before, after, and between existing Sequence children.
- **FR-006**: Users MUST be able to reorder Sequence children through both drag reorder and explicit move commands.
- **FR-007**: Users MUST be able to delete, duplicate, copy, and paste Sequence children using existing designer interaction patterns where those actions are available elsewhere.
- **FR-008**: Selecting a Sequence child MUST open the same activity configuration experience used by other designers.
- **FR-009**: The ordered child activity collection MUST be the source of truth for Sequence execution order.
- **FR-010**: Any visual connections shown between Sequence children MUST be derived from child order and MUST NOT be stored as user-authored arbitrary links.
- **FR-011**: The designer MUST disable manual connection creation entirely in Sequence mode.
- **FR-012**: The designer MUST preserve valid child activity configuration, input/output settings, metadata, validation state, and nested bodies when children are reordered.
- **FR-013**: The designer MUST display validation errors, warnings, incidents, and execution status for Sequence child activities consistently with the enhanced Flowchart designer.
- **FR-014**: Activities with embedded bodies or named outcomes MUST expose compact inline previews in the Sequence view and MUST route editing of those regions through drill-in navigation.
- **FR-015**: Users MUST be able to navigate into embedded child regions and back to the parent Sequence with clear breadcrumbs.
- **FR-016**: The Sequence designer MUST support nested Sequences without losing navigation context.
- **FR-017**: Workflows created outside Studio or before this feature MUST open in the Sequence designer without requiring stored node positions.
- **FR-018**: The designer MUST auto-layout Sequence children into a readable vertical default arrangement and MUST also support a horizontal arrangement.
- **FR-018a**: The designer MUST save the selected Sequence layout orientation in Sequence designer metadata without making layout orientation part of execution semantics.
- **FR-019**: The designer MUST remain usable when the Sequence contains at least 100 child activities.
- **FR-020**: Long activity titles, descriptions, and outcome names MUST not overlap controls or adjacent activities.
- **FR-021**: The designer MUST provide distinct empty, loading, unavailable, validation-failed, and save-failed states.
- **FR-022**: The designer MUST integrate with existing workflow save, publish, undo/redo, dirty-state, and activity picker behavior where those capabilities are available in the designer shell.

### Key Entities *(include if feature involves data)*

- **Sequence Activity**: A container activity whose child activities execute in a defined order.
- **Sequence Child Activity**: An activity contained directly by a Sequence and identified by its position in the ordered child collection.
- **Sequence Order**: The persisted ordering of child activities that determines execution order and drives visual layout.
- **Sequence Designer Metadata**: Non-execution metadata for the Sequence designer, including selected layout orientation.
- **Embedded Region**: A child body, branch, or named outcome owned by an activity inside a Sequence and represented by a compact inline preview plus drill-in affordance.
- **Designer Scope**: The currently visible editable region, such as a parent Sequence, nested body, or branch.
- **Derived Visual Edge**: A non-authoritative visual connector generated from Sequence order for readability.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can add an activity to an empty Sequence, insert an activity between two children, save, reload, and see the same order preserved.
- **SC-002**: Users can reorder a Sequence containing at least 10 child activities using drag reorder and explicit move commands, then save the new order without losing activity configuration.
- **SC-003**: A Sequence containing 100 child activities opens with a readable vertical default auto-layout, can be switched to a horizontal arrangement, and remains interactive for selection, scrolling/panning, and insert operations.
- **SC-003a**: A user can switch a Sequence to horizontal layout, save, reload, and see the horizontal orientation restored while child execution order remains unchanged.
- **SC-004**: At least one activity with named outcomes and one activity with an embedded body show compact inline previews from within a Sequence and can be opened through drill-in navigation for editing.
- **SC-005**: Existing workflows with Sequence activities and no stored Sequence-specific visual positions open without manual repair.
- **SC-006**: Invalid free-form connection attempts inside a Sequence do not produce persisted arbitrary links.
- **SC-007**: Visual states for selected, invalid, faulted, running, completed, and disabled child activities match the enhanced Flowchart designer conventions.
- **SC-008**: The primary Sequence editing flow can be demonstrated without using raw JSON or fallback property editing for the Sequence child list.

## Assumptions

- The enhanced Flowchart designer already uses React Flow and establishes the desired visual and interaction direction.
- JavaScript interoperability with React Flow is acceptable for this feature because it is already used by the enhanced Flowchart designer.
- Sequence should not become a generic Flowchart; its constrained ordered behavior is part of the user-facing feature.
- Layout positions for Sequence children are derived by default and are not required to preserve execution semantics; only the selected layout orientation is saved as Sequence designer metadata.
- The first implementation may reuse existing activity picker, property panel, save/publish, validation, and execution-state services.
- Fine-grained keyboard shortcut behavior should follow existing designer conventions rather than invent a separate shortcut model.
