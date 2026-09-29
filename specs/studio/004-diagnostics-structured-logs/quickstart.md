# Quickstart: Diagnostics Structured Logs Studio Module

## Register Studio module

```csharp
using Elsa.Studio.Diagnostics.StructuredLogs.Extensions;

builder.Services.AddStructuredLogsModule(backendApiConfig);
```

Bundled Studio hosts should reference:

```text
src/modules/Elsa.Studio.Diagnostics.StructuredLogs/Elsa.Studio.Diagnostics.StructuredLogs.csproj
```

## Backend expectation

The active backend must advertise the remote feature:

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

Core implementation is handled in the paired `elsa-core` worker branch; do not modify it from Studio.

## Open page

```text
/diagnostics/structured-logs
```

Workflow-context link:

```text
/diagnostics/structured-logs?workflowInstanceId={workflowInstanceId}
```

Trace/span focused link:

```text
/diagnostics/structured-logs?traceId={traceId}&spanId={spanId}
```

## Manual verification

1. Start a backend with `Elsa.Diagnostics.StructuredLogs` enabled.
2. Start Studio with `AddStructuredLogsModule(backendApiConfig)`.
3. Open `/diagnostics/structured-logs`.
4. Verify the menu label is `Structured Logs` under `Diagnostics`.
5. Verify recent records load before live records.
6. Emit logs with event ID/name, message template, properties, scopes, exception, trace ID, span ID, workflow instance ID, tenant ID, and long source names.
7. Verify rows show timestamp, level, category, source, trace/correlation hint, and rendered message without overflow.
8. Open details and verify template, properties, scopes, exception, source, trace/span, workflow, tenant, and raw JSON/copy output.
9. Change filters and verify REST recent requests, SignalR subscriptions, and URL query parameters update without a full page reload.
10. Disable the remote feature and verify the menu is hidden or direct navigation shows an unavailable state.

## Separation from future modules

This module is not raw stdout/stderr console tailing. A future `Elsa.Studio.Diagnostics.ConsoleLogs` module should own raw console streaming, and a future OpenTelemetry module should own trace waterfalls and metrics.
