# Research: State Machine Designer

## Decision: Dedicated diagram designer provider for `Elsa.StateMachine`

**Rationale**: Existing Studio uses `IDiagramDesignerProvider` implementations to choose a designer by root activity type. Flowchart already has its own provider and fallback covers everything else. A dedicated provider lets StateMachine load only when the backend descriptor exists and keeps root Flowchart behavior isolated.

**Alternatives considered**:

- Extend Flowchart designer to special-case StateMachine: rejected because the data shape is states/transitions, not activities/connections, and changes would risk existing full-screen Flowchart behavior.
- Use fallback property editor only: rejected because it does not meet visual state/transition UX requirements.

## Decision: State-machine-specific mapper and validator

**Rationale**: Backend transitions reference state names through `from` and `to`, while Flowchart connections reference activity IDs and ports. A separate mapper can preserve state/transition order, nested slot payloads, and validation issues without adding brittle conditionals to `FlowchartMapper`.

**Alternatives considered**:

- Reuse `IFlowchartMapper`: rejected because it assumes activity nodes and port connections.
- Map directly in Razor components: rejected because mapping and validation need focused tests.

## Decision: Use state names as MVP graph identity

**Rationale**: Backend PR #7457 stores transition endpoints as state names. Studio must therefore validate empty and duplicate names carefully, but preserving backend identity avoids inventing Studio-only IDs in the first slice.

**Alternatives considered**:

- Add hidden Studio state IDs: deferred because it would require compatibility rules for backend serialization and migration.
- Generate IDs from list index: rejected because reorder and rename behavior would be fragile.

## Decision: Slot editing through existing child activity conventions

**Rationale**: State entry/exit and transition trigger/action are Elsa child activities. Studio should reuse activity picker, activity properties, expression input, and nested designer patterns rather than introducing a custom slot editor model.

**Alternatives considered**:

- Raw JSON slot editor: rejected because it is not a designer UX.
- Embed the existing full-screen Flowchart designer for every child activity: rejected for MVP because it risks disrupting root Flowchart interactions.

## Decision: Implement read-only graph inspection before mutation-heavy editing

**Rationale**: The backend activity contract may still be in PR review. A read-only mapper and provider is the safest first implementation slice, proving activity recognition and graph shape while minimizing serialization risk.

**Alternatives considered**:

- Start with full add/remove editing: deferred until mapper validation and save semantics are covered.
- Wait for backend merge before any Studio planning: rejected because contracts and isolated Studio tasks can be prepared now.
