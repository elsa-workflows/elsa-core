# Diagnostics Console Logs

`Elsa.Diagnostics.ConsoleLogs` is an opt-in module that captures raw `stdout` and `stderr` from the Elsa host process, buffers recent lines, and streams live console output to authorized callers over SignalR. It is intentionally separate from `Elsa.Diagnostics.StructuredLogs`; the two modules cover different diagnostic surfaces.

Start in [src/modules/Elsa.Diagnostics.ConsoleLogs](../../src/modules/Elsa.Diagnostics.ConsoleLogs).

## Scope

This module captures raw process console output only. It does not parse `ILogger` records, write to durable audit storage, call orchestrator log APIs, or implement trace or metric exploration. For semantic `ILogger` capture see [Diagnostics Structured Logs](diagnostics-structured-logs.md).

## Feature Wiring

[ConsoleLogsFeature](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Features/ConsoleLogsFeature.cs):

- registers FastEndpoints assembly
- calls `AddConsoleLogsServices`
- adds FastEndpoints from the module

[AddConsoleLogsServices](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Extensions/ServiceCollectionExtensions.cs) registers:

- SignalR
- `ConsoleLogsOptions`
- source registry
- redactor
- line formatter
- in-memory console log provider
- subscription manager
- `ConsoleCaptureTee` as `IConsoleLogCapture`
- `ConsoleLogCaptureHostedService` hosted service

The hosted service installs a `TextWriter` tee that writes to both the original console destination and the capture pipeline. Original stdout and stderr destinations are preserved.

## Core Contracts

| Contract | Purpose |
| --- | --- |
| [IConsoleLogCapture](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogCapture.cs) | Capture pipeline entry point. |
| [IConsoleLogProvider](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogProvider.cs) | REST/SignalR facade used by endpoints and subscription manager. |
| [IConsoleLogSourceRegistry](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogSourceRegistry.cs) | Tracks source metadata and health. |
| [IConsoleLogRedactor](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogRedactor.cs) | Redacts text before buffering and streaming. |
| [IConsoleLogDroppedLineReporter](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Contracts/IConsoleLogDroppedLineReporter.cs) | Reports dropped-line counts when buffers overflow. |

## Event Flow

Captured console writes pass through redaction before reaching any consumer:

```mermaid
sequenceDiagram
    participant Console as Process stdout/stderr
    participant Tee as ConsoleCaptureTee
    participant Original as Original TextWriter
    participant Redactor as IConsoleLogRedactor
    participant Buffer as ConsoleLineBuffer
    participant Manager as ConsoleLogSubscriptionManager
    participant Hub as ConsoleLogsHub
    participant Client as Authorized caller

    Console->>Tee: write bytes/chars
    Tee->>Original: pass through (preserved)
    Tee->>Redactor: redact line text
    Redactor->>Buffer: append redacted line
    Redactor->>Manager: publish redacted line
    Manager->>Hub: live event
    Hub->>Client: SignalR stream
    Client->>Buffer: recent query through REST
```

## REST And SignalR Surface

REST endpoints:

- `POST /elsa/api/diagnostics/console-logs/recent`
- `GET /elsa/api/diagnostics/console-logs/sources`

Endpoint code is under [Endpoints/ConsoleLogs](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Endpoints/ConsoleLogs).

SignalR:

- Hub: [ConsoleLogsHub](../../src/modules/Elsa.Diagnostics.ConsoleLogs/RealTime/ConsoleLogsHub.cs)
- Route: `/elsa/hubs/diagnostics/console-logs`
- Mapping: [MapConsoleLogsHub](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Extensions/EndpointRouteBuilderExtensions.cs)
- App extension: [UseConsoleLogs](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Extensions/ApplicationBuilderExtensions.cs)

## Authorization

All endpoints and the SignalR hub require `read:diagnostics:console-logs`, defined in [ConsoleLogsPermissions](../../src/modules/Elsa.Diagnostics.ConsoleLogs/Permissions/ConsoleLogsPermissions.cs).

## Safety Boundaries

- Redaction runs before recent buffering, live streaming, and endpoint responses.
- ANSI escape sequences are stripped by default (`StripAnsiEscapeSequences = true`).
- Partial writes (no trailing newline) are buffered until the line completes, reaches the maximum line length, or an idle flush occurs.
- Lines longer than `MaxLineLength` are truncated to one event and marked as truncated.
- Dropped-line counts are reported through `IConsoleLogDroppedLineReporter` when buffers or subscriber queues overflow.

## Configuration

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

Map the live hub after routing is configured:

```csharp
app.UseConsoleLogs();
```

## Design Spec

[specs/006-diagnostics-console-logs/spec.md](../../specs/006-diagnostics-console-logs/spec.md) defines requirements for capture, buffering, endpoints, SignalR, permissions, source identity, and redaction.

## Tests

- [test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests](../../test/unit/Elsa.Diagnostics.ConsoleLogs.UnitTests): capture, filtering, redaction, buffering, source registry, and naming.
- [test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests](../../test/integration/Elsa.Diagnostics.ConsoleLogs.IntegrationTests): module registration, endpoint authorization, SignalR hub behavior, and recent query endpoint.
