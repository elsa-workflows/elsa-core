# Contract: Backend Client

## IConsoleLogsApi

Studio obtains this Refit-style client through `IBackendApiClientProvider`.

```csharp
public interface IConsoleLogsApi
{
    [Post("/diagnostics/console-logs/recent")]
    Task<RecentConsoleLinesResult> GetRecentAsync(
        [Body] ConsoleLogFilter filter,
        CancellationToken cancellationToken = default);

    [Get("/diagnostics/console-logs/sources")]
    Task<ICollection<ConsoleLogSource>> ListSourcesAsync(
        CancellationToken cancellationToken = default);
}
```

## RemoteConsoleLogService

Responsibilities:

- Resolve `IConsoleLogsApi` through `IBackendApiClientProvider`.
- Check the active backend remote feature before optional calls.
- Map UI filter state through `ConsoleLogFilterMapper.ToRecentRequest`.
- Send `source`, `stream`, `text`, `from`, `to`, and clamped `take` values to recent backfill.
- Treat `text` as a server-side filter, not just a client-side highlight.
- Clamp `Take` to the configured local visible-row cap.
- Convert backend absence, authorization failures, validation errors, and network failures into page states instead of unhandled exceptions.
- Avoid direct `HttpClient` construction.

## Remote Feature And Permission

The module is available when the active backend advertises the paired diagnostics console logs remote feature. The expected Studio-facing feature identity is:

```text
Elsa.Diagnostics.ConsoleLogs.ShellFeatures.ConsoleLogs
```

The expected read permission is:

```text
read:diagnostics:console-logs
```

If the feature or permission is unavailable, Studio hides the Diagnostics `Console` navigation item. Direct navigation to `/diagnostics/console` renders unavailable or unauthorized state.

## Canonical Route

Studio route:

```text
/diagnostics/console
```

The Console page remains distinct from `/diagnostics/structured-logs` and does not interpret stdout/stderr rows as structured log records.
