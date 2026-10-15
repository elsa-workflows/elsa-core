# Quickstart: Server Logs Studio Module

## Register the module

```csharp
builder.Services.AddServerLogsModule();
```

If bundled by default, add the module reference from `src/bundles/Elsa.Studio/Elsa.Studio.csproj`.

## Enable the backend

The backend must enable the paired Core feature and map the server logs hub.

## Open the page

Navigate to:

```text
/server-logs
```

## Validate

1. Start Elsa Server with live log streaming enabled.
2. Start Elsa Studio with the Server Logs module.
3. Open Server Logs.
4. Verify recent logs load.
5. Emit a new backend log message.
6. Verify the new row appears live.
7. Select a source and verify rows filter to that source.
8. Open a workflow instance and use the contextual Server Logs action to pre-filter by instance ID.
