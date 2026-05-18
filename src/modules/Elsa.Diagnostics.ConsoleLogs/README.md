# Elsa Diagnostics Console Logs

`Elsa.Diagnostics.ConsoleLogs` is an opt-in Core module for operational diagnostics. It captures raw backend `stdout` and `stderr` lines, preserves the host's original console destinations, redacts data before provider boundaries, and exposes recent plus live console output to authorized callers.

## What It Captures

- Raw `stdout` and `stderr` lines from the current backend process.
- Source metadata for the current process, machine, and container environment when available.
- Recent bounded history and live SignalR events.
- Dropped-line summaries when recent buffers or subscriber queues overflow.

The module is separate from `Elsa.Diagnostics.StructuredLogs`. It does not parse `ILogger` records, provide durable audit storage, call orchestrator log APIs, or implement trace/metric exploration.

## Configure

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

Map the live hub:

```csharp
app.UseConsoleLogs();
```

## Contracts

- Recent lines: `POST /diagnostics/console-logs/recent`
- Sources: `GET /diagnostics/console-logs/sources`
- Live hub: `/elsa/hubs/diagnostics/console-logs`
- Permission: `read:diagnostics:console-logs`

Recent, source, and hub access all require the same permission.

## Safety Boundaries

- Redaction runs before recent buffering, live streaming, endpoint responses, and provider storage.
- ANSI escape sequences are stripped by default.
- Partial writes are buffered until newline, max line length, or idle flush.
- Oversized lines are truncated to one event and marked as truncated.
- Providers receive only redacted line text and redacted source metadata.
