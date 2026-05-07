# Quickstart: Live Server Log Streaming

## Enable the feature

```csharp
services.AddElsa(elsa =>
{
    elsa.UseServerLogStreaming(options =>
    {
        options.RecentLogCapacity = 5_000;
        options.MaxRecentLogQuerySize = 1_000;
        options.SourceHeartbeatTimeout = TimeSpan.FromSeconds(30);
    });
});
```

## Map the hub

```csharp
app.UseServerLogStreaming();
```

This maps `/elsa/hubs/server-logs` and any REST endpoints under the configured Elsa API prefix.

## Authorize users

Grant operational users the `read:server-logs` permission.

## Validate locally

1. Start Elsa Server with the feature enabled.
2. Open Elsa Studio with the paired Studio module installed.
3. Emit an `ILogger` message from the server.
4. Verify it appears in Studio's Server Logs page.
5. Change the level filter to `Warning` and verify lower-level logs are hidden.

## Clustered deployments

The in-memory provider shows logs for the current process only. For merged multi-pod logs, configure a future shared provider that implements `IServerLogProvider`; Studio continues to use the same API and hub contracts.
