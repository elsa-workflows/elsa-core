# Contract: Backend Client

## IServerLogsApi

Refit-style API used through `IBackendApiClientProvider`.

```csharp
public interface IServerLogsApi
{
    [Get("/server-logs/recent")]
    Task<RecentServerLogsResponse> GetRecentAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);

    [Get("/server-logs/sources")]
    Task<ListServerLogSourcesResponse> ListSourcesAsync(CancellationToken cancellationToken = default);
}
```

## RemoteServerLogService

Responsibilities:

- Load sources.
- Load recent logs with filter mapping.
- Clamp client-requested `Take` to the UI row cap.
- Convert backend errors into UI states: unavailable, unauthorized, disconnected, validation error.

## Feature Detection

The module is shown when the backend advertises the remote feature name from Core, expected as `Elsa.ServerLogStreaming` or the final name chosen by Core.

The exact feature name must be aligned during implementation before coding begins.
