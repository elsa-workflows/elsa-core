# State Machine Designer Contract

This contract describes how Studio maps the backend StateMachine activity JSON into designer graph data. It is a Studio-side contract and does not modify elsa-core PR #7457.

## Supported Activity

- Activity type: `Elsa.StateMachine`
- Required backend descriptor: StateMachine activity descriptor present in the activity registry.
- Source contract: elsa-core StateMachine activity with `states`, `transitions`, `initialState`, and `currentState`.

## Input Activity JSON

```json
{
  "type": "Elsa.StateMachine",
  "initialState": "NewOrder",
  "currentState": "NewOrder",
  "states": [
    {
      "name": "NewOrder",
      "entry": { "id": "NewOrderEntry", "nodeId": "StateMachine1:NewOrderEntry", "type": "Elsa.WriteLine", "text": "New order" },
      "exit": { "id": "NewOrderExit", "nodeId": "StateMachine1:NewOrderExit", "type": "Elsa.WriteLine", "text": "Leaving new order" }
    },
    {
      "name": "Paid",
      "entry": { "id": "PaidEntry", "nodeId": "StateMachine1:PaidEntry", "type": "Elsa.WriteLine", "text": "Paid" }
    }
  ],
  "transitions": [
    {
      "name": "MarkPaid",
      "displayName": "Mark paid",
      "from": "NewOrder",
      "to": "Paid",
      "trigger": { "id": "MarkPaidTrigger", "nodeId": "StateMachine1:MarkPaidTrigger", "type": "Elsa.Event" },
      "condition": true,
      "action": { "id": "MarkPaidAction", "nodeId": "StateMachine1:MarkPaidAction", "type": "Elsa.WriteLine", "text": "Payment accepted" }
    }
  ]
}
```

## Graph Mapping

- Each `states[]` item maps to one state node.
- Each `transitions[]` item maps to one directed transition route.
- Route source is `transition.from`; route target is `transition.to`.
- Edge label uses `displayName`, then `name`, then a generated fallback label.
- Terminal state marker is derived from no valid outbound transition targeting an existing state.
- Designer metadata for node position and route vertices may be stored in the activity JSON using existing Studio metadata conventions once implementation identifies the current metadata location.

## Read/Write Guarantees

- Preserve unknown activity-level, state-level, transition-level, and slot-level JSON properties.
- Preserve collection order unless the user explicitly reorders or deletes items.
- Preserve missing optional slots as missing values.
- Preserve missing condition as missing; do not serialize missing condition as `false`.
- Do not mutate root Flowchart `activities` or `connections` when editing a nested StateMachine.

## Validation Contract

Blocking errors:

- Empty state name.
- Duplicate state name.
- Transition missing `from` or `to`.
- Transition source or target does not resolve to exactly one state.

Warnings:

- Missing `initialState`.
- `initialState` or `currentState` references a missing state.
- Unknown child activity descriptor in a slot.
- StateMachine activity descriptor unavailable from backend registry.
