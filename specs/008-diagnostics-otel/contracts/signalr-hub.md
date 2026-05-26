# Contract: SignalR Hub

Hub path:

```text
/elsa/hubs/diagnostics/opentelemetry
```

The hub requires the OpenTelemetry diagnostics view permission.

## Client-to-Server Methods

```text
Subscribe(OpenTelemetryLiveFilter filter)
UpdateSubscription(OpenTelemetryLiveFilter filter)
Unsubscribe()
```

Filters support resource key, service name, trace ID, workflow instance ID, workflow definition ID, severity/status, and signal types.

## Server-to-Client Events

```text
TelemetryReceived(OpenTelemetryStreamItem item)
ResourceChanged(TelemetryResource resource)
DroppedTelemetry(OpenTelemetryDroppedSummary summary)
StorageDiagnosticsChanged(OpenTelemetryStorageDiagnostics diagnostics)
SubscriptionRejected(string reason)
```

`OpenTelemetryStreamItem` variants:

- `TraceUpdated`
- `SpanReceived`
- `MetricPointReceived`
- `LogRecordReceived`

## Lifecycle

- Subscriptions are bounded by server-side per-connection subscriber capacity.
- Filter changes update subscription state without creating duplicate subscriptions.
- Disconnected clients release subscriber resources.
- When a subscriber queue is full, the oldest queued update is dropped, `DroppedLiveUpdateCount` is incremented, and dropped live updates are reported through storage diagnostics or dropped summaries.
- Subscriber overflow must not disconnect the client unless the connection itself is unhealthy or unauthorized.
