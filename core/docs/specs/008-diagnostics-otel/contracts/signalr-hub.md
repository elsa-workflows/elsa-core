# Contract: SignalR Hub

Hub path:

```text
/elsa/hubs/diagnostics/opentelemetry
```

The hub requires the OpenTelemetry diagnostics view permission.

## Client-to-Server Methods

```text
SubscribeAsync(OpenTelemetryTraceFilter filter)
```

Filters support resource ID, service name, trace ID, workflow instance ID, workflow definition ID, status, text, and time range. Metric clients subscribe through the trace-filter-compatible live stream and apply metric-specific filtering client-side.

## Server-to-Client Events

```text
ReceiveAsync(OpenTelemetryStreamItem item)
```

`OpenTelemetryStreamItem` variants:

- `Resource`
- `Trace`
- `MetricPoint`
- `Log`
- `DroppedItems`

## Lifecycle

- Subscriptions are bounded by server-side per-connection subscriber capacity.
- Filter changes are represented by starting a new subscription.
- Disconnected clients release subscriber resources.
- When a subscriber queue is full, live updates are dropped and reported through `DroppedItems` summaries.
- Subscriber overflow must not disconnect the client unless the connection itself is unhealthy or unauthorized.
