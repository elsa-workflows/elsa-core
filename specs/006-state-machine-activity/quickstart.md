# Quickstart: State Machine Activity

1. Add a `StateMachine` with `InitialState = "NewOrder"`.
2. Add states named `NewOrder` and `Paid`.
3. Add an entry activity to `NewOrder`.
4. Add a transition from `NewOrder` to `Paid` with a trigger activity and optional action.
5. Execute the workflow.
6. Verify `NewOrder` entry runs, outbound transition triggers are scheduled, and the workflow remains active.
7. Complete the trigger and verify `CurrentState` becomes `Paid`.
8. Verify a false transition condition re-arms that transition trigger without canceling competing triggers.
9. Verify entering a state with no valid outbound transitions completes the state machine.

Validation command:

```bash
dotnet test test/unit/Elsa.Activities.UnitTests/Elsa.Activities.UnitTests.csproj --no-restore
```
