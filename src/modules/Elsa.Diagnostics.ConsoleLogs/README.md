# Elsa Diagnostics Console Logs

`Elsa.Diagnostics.ConsoleLogs` is an opt-in Core module for operational diagnostics. It hosts `ConsoleLogStreaming.Core`, adds Elsa workflow metadata, and exposes recent plus live console output to authorized callers.

## What It Captures

- Raw `stdout` and `stderr` lines from the current backend process.
- Source metadata for the current process, machine, and container environment when available.
- Recent bounded history and live SignalR events.
- Dropped-line summaries when recent buffers or subscriber queues overflow.

The module is separate from `Elsa.Diagnostics.StructuredLogs`. It does not parse `ILogger` records, provide durable audit storage, call orchestrator log APIs, or implement trace/metric exploration.

Capture, redaction, buffering, source tracking, provider contracts, and live subscriptions come from `ConsoleLogStreaming.Core`. Elsa keeps only the module wiring, workflow metadata accessor, authorization, REST endpoints, SignalR hub, and DTO mapping.

## Configure

```csharp
services.AddElsa(elsa =>
{
    elsa.UseConsoleLogs(options =>
    {
        options.RecentCapacity = 5_000;
        options.SubscriberCapacity = 1_000;
        options.MaxRecentQuerySize = 1_000;
        options.MaxLineLength = 16_384;
        options.PreserveAnsi = true; // pass colors through to consumers
    });
});
```

Map the live hub:

```csharp
app.UseConsoleLogs();
```

## Capturing Colors

The capture passes raw stdout/stderr bytes through to consumers. ANSI escape sequences (colors, cursor
moves) are preserved by default; consumers (e.g. the Studio's `Raw ANSI` toggle) decide whether to render
them or strip them on display.

Whether colors actually arrive at the capture depends on whether the .NET console logger emits them.
`Microsoft.Extensions.Logging.Console`'s `SimpleConsoleFormatter` defaults to `LoggerColorBehavior.Default`,
which suppresses colors whenever `Console.IsOutputRedirected` is `true` — that includes most Docker/Kubernetes
deployments, IDE-hosted runs (JetBrains Rider, VS Code), and any piped stdout. To force colors regardless,
add the following to `appsettings.json` (no module dependency required):

```json
"Logging": {
  "Console": {
    "FormatterOptions": {
      "ColorBehavior": "Enabled"
    }
  }
}
```

With `ColorBehavior = Enabled`, the logger writes ANSI sequences such as `\x1b[32m` and `\x1b[0m` into the
tee, the capture publishes them verbatim, and the Studio renders them.

## Contracts

- Recent lines: `POST /diagnostics/console-logs/recent`
- Sources: `GET /diagnostics/console-logs/sources`
- Live hub: `/elsa/hubs/diagnostics/console-logs`
- Permission: `read:diagnostics:console-logs`

Recent, source, and hub access all require the same permission.

Custom storage providers should implement `ConsoleLogStreaming.Core.IConsoleLogProvider` and use the core models. Elsa-specific values such as workflow instance IDs are represented as metadata internally and projected onto shared `ConsoleLogStreaming.Contracts` DTOs at the REST and SignalR boundaries.

## Safety Boundaries

- Redaction runs before recent buffering, live streaming, endpoint responses, and provider storage.
- ANSI escape sequences are preserved by default; set `PreserveAnsi = false` to strip them server-side.
- Partial writes are buffered until newline, max line length, or idle flush.
- Oversized lines are truncated to one event and marked as truncated.
- Core providers receive only redacted line text and redacted source metadata.
