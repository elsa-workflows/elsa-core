# Elsa.Studio.Diagnostics.StructuredLogs

Elsa.Studio.Diagnostics.StructuredLogs adds a diagnostics structured log viewer to Elsa Studio. It loads recent semantic log records, subscribes to live updates over SignalR, and lets operators inspect rendered messages, templates, properties, scopes, exceptions, source metadata, trace/span IDs, workflow context, tenant, and correlation fields.

This module is not raw stdout/stderr console tailing. A future `Elsa.Studio.Diagnostics.ConsoleLogs` module should own raw console streaming, and a future OpenTelemetry module should own trace waterfalls and metrics.

## Register The Module

Add the module to the Studio host:

```csharp
builder.Services.AddStructuredLogsModule(backendApiConfig);
```

Bundled Studio hosts should reference `src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Elsa.Studio.Diagnostics.StructuredLogs.csproj` and call `AddStructuredLogsModule(backendApiConfig)`.

## Backend Requirement

The paired Elsa Core backend must advertise:

```text
Elsa.Diagnostics.StructuredLogs
```

And expose:

```text
POST /diagnostics/structured-logs/recent
GET  /diagnostics/structured-logs/sources
GET  /diagnostics/structured-logs/storage
HUB  /elsa/hubs/diagnostics/structured-logs
```

## Route

Open the viewer at:

```text
/diagnostics/structured-logs
```

Workflow instance screens can deep-link to:

```text
/diagnostics/structured-logs?workflowInstanceId={workflowInstanceId}
```

Trace/span focused links can use:

```text
/diagnostics/structured-logs?traceId={traceId}&spanId={spanId}
```

## Operator Workflow

- Recent structured logs load when the page opens.
- A live SignalR subscription starts after the initial backfill.
- Pause freezes new rows locally without disconnecting.
- Reconnect restarts the live observer.
- Clear removes local rows without clearing backend history.
- Copy selected, copy visible, and details copy actions provide fast handoff for support/debugging.
- Source selection defaults to all sources and can focus on one backend process, pod, or container.
- Storage write drops are shown when the backend reports pressure in a durable storage queue.

## Validation

1. Start an Elsa backend with `Elsa.Diagnostics.StructuredLogs` enabled.
2. Start Studio with `AddStructuredLogsModule`.
3. Open `/diagnostics/structured-logs`.
4. Verify recent rows load or the empty state appears.
5. Emit an `ILogger` message with a template, properties, scope, trace ID, and span ID and verify it appears live.
6. Open details and verify rendered message, template, properties, scopes, exception, source, trace/span, workflow, tenant, correlation, and raw JSON are visible.
7. Apply filters and verify recent/live rows are scoped to the chosen values.
8. Open a workflow instance and use the Structured Logs action to confirm the workflow filter is applied.
