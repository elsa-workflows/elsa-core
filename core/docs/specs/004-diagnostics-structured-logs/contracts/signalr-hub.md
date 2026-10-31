# SignalR Hub Contract: Diagnostics Structured Logs

## Hub Route

`/elsa/hubs/diagnostics/structured-logs`

Requires an authenticated user authorized for `read:diagnostics:structured-logs`.

## Client-to-Server Methods

### SubscribeAsync

```csharp
Task SubscribeAsync(StructuredLogFilter filter);
```

Creates or replaces the caller's live structured-log subscription.

### UpdateFilterAsync

```csharp
Task UpdateFilterAsync(StructuredLogFilter filter);
```

Replaces the caller's active filter without disconnecting.

### UnsubscribeAsync

```csharp
Task UnsubscribeAsync();
```

Stops the caller's active subscription and releases its resources.

## Server-to-Client Methods

### ReceiveLogEventAsync

Payload is `StructuredLogEvent`.

```csharp
Task ReceiveLogEventAsync(StructuredLogEvent logEvent);
```

### ReceiveDroppedEventsAsync

Payload is `StructuredLogDroppedEventSummary`.

```csharp
Task ReceiveDroppedEventsAsync(StructuredLogDroppedEventSummary summary);
```

### ReceiveSourceChangedAsync

Payload is `StructuredLogSource`.

```csharp
Task ReceiveSourceChangedAsync(StructuredLogSource source);
```

## Backpressure

Subscriber queues remain bounded. When a subscriber cannot keep up, the provider drops events for that subscriber and later sends a dropped-event summary.
