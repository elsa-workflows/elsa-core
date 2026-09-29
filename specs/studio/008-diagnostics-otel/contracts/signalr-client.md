# Contract: SignalR Client

Hub path:

```text
/elsa/hubs/diagnostics/opentelemetry
```

Studio connects with existing authenticated SignalR configuration.

## Client-to-Server Methods

```text
Subscribe(OpenTelemetryLiveFilter filter)
UpdateSubscription(OpenTelemetryLiveFilter filter)
Unsubscribe()
```

The filter supports resource key, service name, trace ID, workflow instance ID, workflow definition ID, severity/status, and signal types.

## Server-to-Client Events

```text
TelemetryReceived(OpenTelemetryStreamItem item)
ResourceChanged(TelemetryResource resource)
DroppedTelemetry(OpenTelemetryDroppedSummary summary)
StorageDiagnosticsChanged(OpenTelemetryStorageDiagnostics diagnostics)
SubscriptionRejected(string reason)
```

`OpenTelemetryStreamItem` is a discriminated item:

```text
TraceUpdated
SpanReceived
MetricPointReceived
LogRecordReceived
```

## Lifecycle Rules

- Studio starts live subscription after recent data loads.
- Filter changes update the subscription without rebuilding the whole page.
- Backend switching and component disposal stop the hub connection.
- Reconnect attempts surface `reconnecting`, `connected`, and `disconnected` states.
- UI buffers are capped independently from backend store capacity.
- When a UI buffer exceeds its configured cap, Studio prunes or drops the oldest visible live items first and surfaces an overflow/drop indicator instead of growing memory without bounds.
