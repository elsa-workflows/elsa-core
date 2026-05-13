# Elsa.Diagnostics.StructuredLogs

Elsa.Diagnostics.StructuredLogs provides live structured log streaming for Elsa hosts. It captures `ILogger` events, redacts sensitive values, keeps a bounded recent-log buffer, exposes REST endpoints for recent logs and sources, and streams live events to Studio over SignalR.

This module captures semantic `ILogger` records only. Direct stdout/stderr console streaming belongs to a future diagnostics console logs module, and trace waterfalls, metrics, and span exploration belong to a future diagnostics OpenTelemetry module.

## Enable The Feature

Register the module with Elsa:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseStructuredLogs(options =>
    {
        options.RecentLogCapacity = 5_000;
        options.MaxRecentLogQuerySize = 1_000;
        options.SourceHeartbeatTimeout = TimeSpan.FromSeconds(30);
    });
});
```

Then map the HTTP endpoints and SignalR hub:

```csharp
app.UseStructuredLogs();
```

This maps the structured logs hub at `/elsa/hubs/diagnostics/structured-logs` and the REST endpoints under the configured Elsa API prefix.

## Authorization

The recent-log endpoint, source-list endpoint, and storage-diagnostics endpoint require the `read:diagnostics:structured-logs` permission. The SignalR hub requires an authenticated user, matching the existing Elsa workflow hub authorization pattern. Grant `read:diagnostics:structured-logs` only to operators and developers who are allowed to inspect backend logs.

## Studio Integration

Elsa Studio can use this module to show:

- A recent log backfill when the page opens.
- Live log events as the server emits them.
- Level, category, message, tenant, workflow, trace, correlation, source, and time filters.
- Cluster/source metadata such as source ID, pod name, namespace, container name, node name, machine name, process ID, and source health.
- Storage pressure metadata such as dropped durable write counts when a configured store reports them.

## Clustered Deployments

The default in-memory provider captures logs for the current process only. It still includes source identity and Kubernetes/container metadata so Studio can filter and display the active source.

For persisted logs, add a storage package such as `Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite` and opt in from the structured logs feature:

```csharp
services.AddElsa(elsa =>
{
    elsa.UseStructuredLogs(structuredLogs =>
    {
        structuredLogs.UseSqliteStorage("Data Source=elsa-structured-logs.db");
    });
});
```

The core module remains storage-provider neutral. Custom stores can replace `IStructuredLogStore` while live updates continue through `IStructuredLogLiveFeed`; Studio continues to use the same REST and SignalR contracts, including source filtering, source-change notifications, and storage diagnostics.

## Redaction

Log events pass through `IStructuredLogRedactor` before they are buffered or streamed. Configure `StructuredLogsOptions` to extend the default sensitive property names and text patterns.

## Local Validation

1. Start Elsa Server with `UseStructuredLogs` enabled.
2. Open Elsa Studio with the paired Structured Logs module installed.
3. Emit an `ILogger` message from the server.
4. Verify the message appears in Studio's Structured Logs page.
5. Change the level filter to `Warning` and verify lower-level logs are hidden.
6. In containerized environments, verify the source list shows pod/container metadata.
