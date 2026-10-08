# Quickstart: State Machine Activity

## Build a StateMachine

1. Add a `StateMachine` with a non-empty `InitialState`.
2. Add uniquely named states. Each state can have an optional `Entry` and `Exit` activity.
3. Add transitions in declaration order. Each transition names its source and target and can contain one optional `Trigger`, `Condition`, and `Action` activity.
4. Use `Sequence` when a slot must run more than one activity; Trigger and Action are scalar activity slots, not arrays.

## Execution contract

After the initial state's Entry completes, outbound transitions are considered in declaration order:

1. Triggerless transitions are evaluated immediately.
2. A missing Condition is true.
3. A false triggerless Condition falls through to the next transition. If eventful alternatives exist, their triggers are scheduled.
4. When a transition is accepted, execution is source `Exit` → transition `Action` → target `Entry`.
5. Self-transitions run the same full Exit → Action → Entry lifecycle.
6. Entering a state with no outbound transitions completes the StateMachine.

If every outbound transition is triggerless and false, the StateMachine remains active without scheduling a busy loop. Reachable triggerless cycles yield through Elsa's scheduler between transitions so they do not grow the synchronous call stack.

## Durable and competing triggers

- A winning transition cancels distinct competing trigger work before source Exit begins.
- If an eventful Trigger completes but its Condition is false, Elsa re-arms that rejected Trigger and preserves already-active competitors. This is an intentional Elsa durability deviation from WF4's cancel-all-and-reschedule behavior.
- Inline StateMachine JSON cannot preserve WF4 shared Trigger object identity. Elsa 3.8 rejects shared Trigger instances and duplicate trigger activity IDs explicitly instead of guessing identity from IDs.
- Continuation tags use declaration-order transition identity. Reordering transitions while an instance is suspended is a compatibility-sensitive definition change.

These boundaries are part of the accepted StateMachine semantics ADR and are observable through validation and regression tests.

## Validation

Run the focused StateMachine tests:

```bash
dotnet test test/unit/Elsa.Activities.UnitTests/Elsa.Activities.UnitTests.csproj --no-restore --filter FullyQualifiedName~StateMachine
```

The suite covers triggerless and eventful paths, missing and false conditions, lifecycle ordering, self-transitions, competing-trigger cancellation, identity rejection, persistence/resumption, and automatic-cycle scheduler yielding.
