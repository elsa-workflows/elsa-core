# Contract: Backend Client

## IStructuredLogsApi

Studio obtains this Refit client through `IBackendApiClientProvider`.

```csharp
public interface IStructuredLogsApi
{
    [Post("/diagnostics/structured-logs/recent")]
    Task<RecentStructuredLogsResult> GetRecentAsync(
        [Body] StructuredLogFilter filter,
        CancellationToken cancellationToken = default);

    [Get("/diagnostics/structured-logs/sources")]
    Task<ICollection<StructuredLogSource>> ListSourcesAsync(
        CancellationToken cancellationToken = default);
}
```

## RemoteStructuredLogService

Responsibilities:

- Resolve `IStructuredLogsApi` through `IBackendApiClientProvider`.
- Map UI filters through `StructuredLogFilterMapper.ToRecentRequest`.
- Clamp `Take` to the current UI row cap.
- Return an empty source list when the sources endpoint is unavailable, while surfacing non-availability/error states to the page where appropriate.
- Avoid direct `HttpClient` construction.

## Remote Feature

The module is available when the active backend advertises:

```text
Elsa.Diagnostics.StructuredLogs
```

If the feature is absent, the menu item is hidden. Direct navigation should render a clear unavailable state or fail through the existing page state without unhandled exceptions.

## Canonical Route

Studio route:

```text
/diagnostics/structured-logs
```

Primary URL query parameters:

```text
sourceId
level
levels
categoryPrefix
text
tenantId
workflowDefinitionId
workflowInstanceId
traceId
spanId
correlationId
from
to
```

The old development route `/server-logs` may redirect to the canonical route if this can be done without retaining old active module identity.
