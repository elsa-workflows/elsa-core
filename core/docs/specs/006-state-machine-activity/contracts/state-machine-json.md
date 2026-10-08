# State Machine JSON Contract

The backend activity accepts the following shape when serialized through Elsa's existing activity JSON pipeline:

```json
{
  "type": "StateMachine",
  "initialState": "NewOrder",
  "currentState": "NewOrder",
  "states": [
    {
      "name": "NewOrder",
      "entry": { "type": "WriteLine", "text": "New order" },
      "exit": { "type": "WriteLine", "text": "Leaving new order" }
    },
    {
      "name": "Paid",
      "entry": { "type": "WriteLine", "text": "Paid" }
    }
  ],
  "transitions": [
    {
      "name": "MarkPaid",
      "displayName": "Mark paid",
      "from": "NewOrder",
      "to": "Paid",
      "trigger": { "type": "Event" },
      "condition": true,
      "action": { "type": "WriteLine", "text": "Payment accepted" }
    }
  ]
}
```

The exact activity payloads inside `entry`, `exit`, `trigger`, and `action` are resolved by Elsa's existing activity serialization. A target state without valid outbound transitions is terminal. A transition whose condition evaluates false leaves the source state current and keeps that transition trigger active for a future attempt.
