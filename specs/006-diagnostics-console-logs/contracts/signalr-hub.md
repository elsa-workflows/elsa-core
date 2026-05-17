# SignalR Hub Contract: Diagnostics Console Logs

## Hub Route

`/elsa/hubs/diagnostics/console-logs`

Requires an authenticated user authorized for `read:diagnostics:console-logs`.

## Client-to-Server Methods

### SubscribeAsync

```csharp
Task SubscribeAsync(ConsoleLogFilter filter);
```

Creates or replaces the caller's live console-log subscription.

### UpdateFilterAsync

```csharp
Task UpdateFilterAsync(ConsoleLogFilter filter);
```

Replaces the caller's active filter without reconnecting.

### UnsubscribeAsync

```csharp
Task UnsubscribeAsync();
```

Stops the caller's active subscription and releases its resources.

## Server-to-Client Methods

### ReceiveConsoleLogLineAsync

Payload is `ConsoleLogLine`.

```csharp
Task ReceiveConsoleLogLineAsync(ConsoleLogLine line);
```

### ReceiveDroppedLinesAsync

Payload is `ConsoleLogDroppedSummary`.

```csharp
Task ReceiveDroppedLinesAsync(ConsoleLogDroppedSummary summary);
```

### ReceiveSourceChangedAsync

Payload is `ConsoleLogSource`.

```csharp
Task ReceiveSourceChangedAsync(ConsoleLogSource source);
```

## Backpressure

Subscriber queues remain bounded. When a subscriber cannot keep up, the provider drops live lines for that subscriber and sends a dropped-line summary when possible.

## Filtering

Live subscriptions support source ID, stream, free-text query, time range, and server-clamped limits where applicable. Filter updates take effect without requiring reconnect.
