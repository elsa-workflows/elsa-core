# Research: State Machine Activity

## Decision: Keep the first implementation server-side only

**Rationale**: Issue #5085 includes backend activity semantics and a large Studio designer effort. The backend can be delivered independently in `elsa-core`; designer support belongs in separate Studio work.

**Alternatives considered**: Implementing designer changes in the same change was rejected because it crosses repository and product boundaries and is materially larger than the runtime activity.

## Decision: Reference states by name in transitions

**Rationale**: The issue sample JSON uses `from` and `to` state names. Name references are stable in persisted JSON and are simple for an initial backend model.

**Alternatives considered**: Direct object references were rejected because transition objects may be serialized separately from state objects and because name references match the issue contract.

## Decision: Use existing child scheduling and cancellation behavior

**Rationale**: Elsa already cancels child activity contexts and clears bookmarks when an activity context is canceled. A state machine can schedule each trigger as a child and cancel losing trigger contexts when one transition wins.

**Alternatives considered**: Adding a separate trigger registry was rejected as unnecessary for a first implementation.
