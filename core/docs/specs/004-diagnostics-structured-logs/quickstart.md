# Quickstart: Diagnostics Structured Logs

## Configure the module

```csharp
services.AddElsa(elsa =>
{
    elsa.UseStructuredLogs(options =>
    {
        options.RecentLogCapacity = 5_000;
        options.SubscriberChannelCapacity = 1_000;
        options.MaxRecentLogQuerySize = 1_000;
    });
});
```

## Map the hub

```csharp
app.UseStructuredLogs();
```

This maps the SignalR hub at `/elsa/hubs/diagnostics/structured-logs`. FastEndpoints maps recent-log and source-list endpoints under the configured Elsa API prefix at `/diagnostics/structured-logs/recent` and `/diagnostics/structured-logs/sources`.

## Shell feature configuration

Shell-based hosts enable the feature with the diagnostics structured logs shell feature name:

```json
{
  "ShellFeatures": {
    "Elsa.Diagnostics.StructuredLogs.ShellFeatures.StructuredLogsFeature": {
      "RecentLogCapacity": 5000,
      "SubscriberChannelCapacity": 1000,
      "MaxRecentLogQuerySize": 1000,
      "IncludeStructuredLogsInternalLogs": false
    }
  }
}
```

## Authorization

Grant operational users the `read:diagnostics:structured-logs` permission.

## What this module captures

The module captures structured `ILogger` records, including rendered messages, message templates, named properties, active scopes, exceptions, source metadata, and trace/span IDs when available.

Direct stdout/stderr writes are out of scope for this module and belong to a future diagnostics console logs module. Trace waterfalls, metric charts, and span exploration belong to a future diagnostics OpenTelemetry module.

## Validation

Run targeted checks after implementation:

```bash
dotnet build src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj
dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Elsa.Diagnostics.StructuredLogs.UnitTests.csproj
dotnet test test/integration/Elsa.Diagnostics.StructuredLogs.IntegrationTests/Elsa.Diagnostics.StructuredLogs.IntegrationTests.csproj
```

Validation on 2026-05-10:

- `dotnet build src/modules/Elsa.Diagnostics.StructuredLogs/Elsa.Diagnostics.StructuredLogs.csproj` passed.
- `dotnet test test/unit/Elsa.Diagnostics.StructuredLogs.UnitTests/Elsa.Diagnostics.StructuredLogs.UnitTests.csproj` passed with 28 tests.
- `dotnet test test/integration/Elsa.Diagnostics.StructuredLogs.IntegrationTests/Elsa.Diagnostics.StructuredLogs.IntegrationTests.csproj` passed with 6 tests.
