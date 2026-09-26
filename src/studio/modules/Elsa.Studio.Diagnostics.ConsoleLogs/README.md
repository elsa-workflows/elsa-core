# Elsa Studio Diagnostics Console Logs

Provides the Studio diagnostics Console page for raw stdout and stderr streaming.

## Route

```text
/diagnostics/console
```

## Backend Capability

The module is remote-feature gated by:

```text
Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogs
```

The expected backend read permission is:

```text
read:diagnostics:console-logs
```

When the feature or permission is unavailable, Studio hides the Diagnostics `Console` navigation item. Direct route visits show unavailable or unauthorized page states.

## Contracts

- Recent lines: `POST /diagnostics/console-logs/recent`
- Sources: `GET /diagnostics/console-logs/sources`
- Live hub: `hubs/diagnostics/console-logs` resolved from `IBackendApiClientProvider.Url`

With the default backend API base ending in `/elsa/api`, the relative hub URL resolves to `/elsa/hubs/diagnostics/console-logs`, matching the existing Structured Logs hub pattern. The live hub is authenticated through `IHttpConnectionOptionsConfigurator`, and REST calls use `IBackendApiClientProvider`.

## Structured Logs Distinction

Console rows are raw stdout/stderr text. This module does not parse rows into structured log records and does not expose semantic fields such as level, category, template, properties, scopes, trace ID, or span ID.
