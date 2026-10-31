# Contract: SignalR Client

## Hub URL

Studio derives the hub URL from the configured backend API URL by using the same backend origin and replacing the API path with the canonical hub path:

```text
{backend-origin}/elsa/hubs/diagnostics/structured-logs
```

The connection must be configured with `IHttpConnectionOptionsConfigurator` before starting.

## Client observer

`IStructuredLogObserver` responsibilities:

- `StartAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)`
- `UpdateFilterAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)`
- `ReconnectAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default)`
- `DisposeAsync()`
- Publish connection status changes to the page.
- Raise live event, dropped-event, and source-change callbacks on background SignalR messages.
- Re-send the active filter after automatic reconnect.
- Send `UnsubscribeAsync` before disposal when connected.

## Server methods invoked by Studio

```text
SubscribeAsync(StructuredLogFilter filter)
UpdateFilterAsync(StructuredLogFilter filter)
UnsubscribeAsync()
```

## Client methods invoked by backend

```text
ReceiveLogEventAsync(StructuredLogEvent logEvent)
ReceiveDroppedEventsAsync(StructuredLogDroppedEventSummary summary)
ReceiveSourceChangedAsync(StructuredLogSource source)
```

## Status handling

- `404` while starting the hub maps to `Unavailable`.
- `401` while starting the hub maps to `Unauthorized`.
- Automatic reconnect maps to `Reconnecting`, then `Connected` after resubscribe succeeds.
- Closed/disposed connections map to `Disconnected`.
