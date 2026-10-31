# Data Model: State Machine Activity

## StateMachine

- `States`: Ordered collection of state definitions.
- `Transitions`: Ordered collection of directed transition definitions.
- `InitialState`: Name of the first state.
- `CurrentState`: Name of the active state, updated as transitions complete. A current state with no valid outbound transitions is terminal.

## State

- `Name`: Unique state name by convention; duplicate names resolve to the first declaration.
- `Entry`: Optional activity scheduled when entering the state.
- `Exit`: Optional activity scheduled when leaving the state.

## Transition

- `Name`: Optional machine-readable transition name.
- `DisplayName`: Optional human-readable transition name.
- `From`: Source state name.
- `To`: Target state name.
- `Trigger`: Optional activity that waits for the transition event or condition source. If its condition evaluates false, the trigger remains active for a future attempt.
- `Condition`: Optional boolean expression input. Missing condition means true; false means the source state remains current and the same transition trigger is re-armed.
- `Action`: Optional activity scheduled after the trigger wins and before source exit.

## State Transitions

```text
EnterState -> Entry -> OutboundTriggers
EnterState -> Entry -> NoValidOutboundTransitions -> CompleteStateMachine
OutboundTrigger -> ConditionFalse -> ReArmFailedTrigger + OutboundTriggersRemainPending
OutboundTrigger -> ConditionTrue -> CancelCompetingTriggers -> Action -> SourceExit -> TargetEntry -> OutboundTriggers
```
