# Data Model: State Machine Designer

## StateMachine Activity

- `type`: Expected activity type name `Elsa.StateMachine`.
- `states`: Ordered collection of state declarations.
- `transitions`: Ordered collection of transition declarations.
- `initialState`: Optional name of the first state.
- `currentState`: Optional name of the active state when viewing persisted or running definitions.

Validation rules:

- Missing or unknown activity type disables this designer.
- `initialState` and `currentState` should reference existing state names when set.
- Unknown extra activity properties must be preserved.

## State Node

- `name`: User-visible and backend-referenced state name.
- `entry`: Optional child activity slot.
- `exit`: Optional child activity slot.
- `position`: Studio designer metadata for graph layout.
- `isTerminal`: Derived from absence of valid outbound transitions.
- `validationIssues`: Problems such as empty name, duplicate name, or broken references.

Validation rules:

- Names should be non-empty and unique for reliable editing.
- Duplicate names are invalid for Studio editing because backend transition references cannot distinguish them.
- Entry and exit slot payloads must be preserved even if their activity descriptor is unknown.

## Transition Route

- `name`: Optional machine-readable name.
- `displayName`: Optional label shown on the transition route.
- `from`: Source state name.
- `to`: Target state name.
- `trigger`: Optional child activity slot.
- `condition`: Optional Elsa boolean input.
- `action`: Optional child activity slot.
- `vertices`: Optional Studio designer metadata for future route drawing.
- `validationIssues`: Problems such as missing source, missing target, empty endpoint, or duplicate ambiguous endpoint.

Validation rules:

- Source and target names must resolve to known, unique states before save.
- Missing condition means true and must be preserved as missing, not serialized as false.
- Trigger and action payloads must remain attached to the same transition when graph layout changes.

## Slot Activity

- Child Elsa activity JSON object embedded in a state entry/exit slot or transition trigger/action slot.
- May have its own properties, nested activities, and designer metadata.

Validation rules:

- Unknown child activity descriptors are shown as unavailable placeholders and preserved.
- Editing a slot updates only its owning state or transition.

## Validation Issue

- `severity`: Error or warning.
- `code`: Stable issue identifier.
- `message`: User-facing summary.
- `target`: State, transition, or slot that owns the issue.

Validation rules:

- Errors block save where Studio can prevent malformed state references.
- Warnings allow save while preserving unknown backend-compatible payloads.

## State Transitions

```text
LoadActivityJson -> MapStatesAndTransitions -> ValidateGraph -> RenderGraph
RenderGraph -> EditStateOrTransition -> ValidateGraph -> ReadActivityJson
ReadActivityJson -> PreserveSlotsAndUnknownProperties -> SaveWorkflowDefinition
```
