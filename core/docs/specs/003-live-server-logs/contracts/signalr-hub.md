# Contract: Server Logs SignalR Hub

## Endpoint

`/elsa/hubs/server-logs`

## Authorization

Requires `read:server-logs`.

## Client-to-server methods

### SubscribeAsync

Starts or replaces the caller's active subscription.

Request:

```json
{
  "minimumLevel": "Information",
  "levels": ["Warning", "Error"],
  "categoryPrefix": "Elsa.",
  "text": "bookmark",
  "tenantId": "tenant-a",
  "workflowDefinitionId": "wf-def",
  "workflowInstanceId": "wf-inst",
  "traceId": "trace",
  "correlationId": "corr",
  "sourceId": "pod-a",
  "from": "2026-05-06T10:00:00Z"
}
```

### UpdateFilterAsync

Updates the caller's active filter without reconnecting. Same payload as `SubscribeAsync`.

### UnsubscribeAsync

Stops streaming events to the caller.

## Server-to-client methods

### ReceiveLogEventAsync

Payload is `ServerLogEvent`.

### ReceiveDroppedEventsAsync

```json
{
  "sourceId": "pod-a",
  "droppedCount": 42,
  "reason": "SubscriberBackpressure"
}
```

### ReceiveSourceChangedAsync

Payload is `ServerLogSource`.

## Error behavior

- Invalid filters produce a hub exception with validation details.
- Unauthorized callers are rejected during hub connection.
- Provider failures emit a terminal hub error and close the subscription.
