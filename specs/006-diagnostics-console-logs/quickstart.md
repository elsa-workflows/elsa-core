# Quickstart: Diagnostics Console Logs

## Configure the module

```csharp
services.AddElsa(elsa =>
{
    elsa.UseConsoleLogs(options =>
    {
        options.RecentLogCapacity = 5_000;
        options.SubscriberChannelCapacity = 1_000;
        options.MaxRecentQuerySize = 1_000;
        options.MaxLineLength = 16_384;
        options.StripAnsiEscapeSequences = true;
    });
});
```

## Map the hub

```csharp
app.UseConsoleLogs();
```

This maps the SignalR hub at `/elsa/hubs/diagnostics/console-logs`. FastEndpoints maps recent-line and source-list endpoints under the configured Elsa API prefix at `/diagnostics/console-logs/recent` and `/diagnostics/console-logs/sources`.

## Shell feature configuration

Shell-based hosts enable the feature with the diagnostics console logs shell feature name:

```json
{
  "ShellFeatures": {
    "Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogsFeature": {
      "RecentLogCapacity": 5000,
      "SubscriberChannelCapacity": 1000,
      "MaxRecentQuerySize": 1000,
      "MaxLineLength": 16384,
      "IdleFlushTimeout": "00:00:01",
      "StripAnsiEscapeSequences": true,
      "IncludeConsoleLogsInternalLogs": false
    }
  }
}
```

## Authorization

Grant operational users the `read:diagnostics:console-logs` permission. Recent lines, source listing, and live hub access all require this same permission.

## What this module captures

The module captures raw stdout and stderr lines from the backend process while preserving the host's original console destinations. It emits line-oriented, source-aware, redacted console log events.

This module does not parse structured `ILogger` events, persist durable audit logs, call Kubernetes or Docker log APIs, integrate vendor sinks, or implement OpenTelemetry trace/metric exploration.

## Validation

Run targeted checks after implementation:

```bash
dotnet build src/modules/Elsa.Diagnostics.ConsoleLogs/Elsa.Diagnostics.ConsoleLogs.csproj
dotnet test test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Elsa.Diagnostics.ConsoleLogs.UnitTests.csproj
dotnet test test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/Elsa.Diagnostics.ConsoleLogs.IntegrationTests.csproj
```

Validation on 2026-05-18:

- `dotnet test test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests/Elsa.Diagnostics.ConsoleLogs.UnitTests.csproj` passed with 23 tests.
- `dotnet test test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests/Elsa.Diagnostics.ConsoleLogs.IntegrationTests.csproj` passed with 9 tests.
- Boundary scan found only explicit out-of-scope references and `Sequence`/`EscapeSequences` identifier matches.
