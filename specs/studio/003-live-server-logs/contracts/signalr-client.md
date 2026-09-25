# Contract: SignalR Client

## Hub URL

Build from `IBackendApiClientProvider.Url`:

```text
{backendUrl}/hubs/server-logs
```

This mirrors the existing workflow instance observer convention.

## Authentication

Use `IHttpConnectionOptionsConfigurator.ConfigureAsync` when constructing the `HubConnection`.

## Client Observer

`IServerLogObserver` responsibilities:

- Start connection.
- Subscribe with filter.
- Update filter.
- Raise events for log rows, dropped-event summaries, source changes, and connection status changes.
- Reconnect or surface reconnection state.
- Dispose the hub connection.

## Server Methods

- `SubscribeAsync(filter)`
- `UpdateFilterAsync(filter)`
- `UnsubscribeAsync()`

## Client Methods

- `ReceiveLogEventAsync(event)`
- `ReceiveDroppedEventsAsync(summary)`
- `ReceiveSourceChangedAsync(source)`
