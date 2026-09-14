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

## Historical Validation Notes

These results belong to the earlier `Elsa.ServerLogs` implementation. The commands are preserved as recorded, and the referenced test projects no longer exist under those names.

- `dotnet test test/unit/Elsa.ServerLogs.UnitTests/Elsa.ServerLogs.UnitTests.csproj --no-restore` passed with 22 server logs tests.
- `dotnet build src/modules/Elsa.ServerLogs/Elsa.ServerLogs.csproj --no-restore` passed.
- `dotnet restore src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj` passed.
- `dotnet build src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj --no-restore` passed.
- `dotnet test test/integration/Elsa.ServerLogs.IntegrationTests/Elsa.ServerLogs.IntegrationTests.csproj --no-restore` passed with 4 server logs smoke tests.
- At the time, the commands reported existing repository warnings, including `NU1903` for `Snappier` and nullable/analyzer warnings in unrelated modules.

## Clustered deployments

The in-memory provider shows logs for the current process only. For merged multi-pod logs, configure a future shared provider that implements `IServerLogProvider`; Studio continues to use the same API and hub contracts.
