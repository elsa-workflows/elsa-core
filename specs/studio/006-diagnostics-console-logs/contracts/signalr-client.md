# Contract: SignalR Client

## Hub URL

Build from `IBackendApiClientProvider.Url`:

```text
new Uri(backendUrl, "hubs/diagnostics/console-logs")
```

With the default Studio backend API base ending in `/elsa/api`, this resolves to `/elsa/hubs/diagnostics/console-logs`.

The hub connection must be authenticated by passing the captured `HttpConnectionOptions` through `IHttpConnectionOptionsConfigurator.ConfigureAsync`.

## IConsoleLogObserver

Responsibilities:

- Start the authenticated live connection.
- Subscribe with the current `ConsoleLogFilter`.
- Send server-side `source`, `stream`, `text`, `from`, and `to` filters.
- Update the live subscription when filters change.
- Raise events for received lines, dropped-line summaries, source changes, and connection status changes.
- Reconnect or surface reconnecting/disconnected state.
- Preserve active filters and URL state across reconnect actions.
- Unsubscribe and dispose the hub connection when the page is disposed, the selected backend changes, or the user leaves the active stream.

## Server Methods

```text
SubscribeAsync(filter)
UpdateFilterAsync(filter)
UnsubscribeAsync()
```

## Client Methods

```text
ReceiveConsoleLineAsync(line)
ReceiveDroppedLinesAsync(summary)
ReceiveSourceChangedAsync(source)
```

## Connection States

The observer reports:

```text
Connecting
Connected
Reconnecting
Disconnected
Unavailable
Unauthorized
```

The page combines observer states with local viewer states such as loading recent lines, paused, empty, no matches, stale source, and partial data due to dropped or truncated lines.

## Reconnect Behavior

After automatic or user-triggered reconnect, the observer resubscribes using the latest filter and keeps `source.id`, stream set, server-side `text`, and UTC time range intact.
