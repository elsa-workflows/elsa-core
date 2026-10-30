# Quickstart: Weaver Copilot UI

1. Start an Elsa Core backend on the configured Studio backend URL with the AI host feature enabled.
2. Start Studio Server:

```bash
dotnet run --project src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj --framework net10.0
```

3. Open Studio and navigate to `AI > Weaver`, or open `/ai/weaver` directly.
4. Confirm the capability pill reports available state when Core exposes `/ai/capabilities`.
5. Attach a workflow definition or workflow instance reference id when the backend advertises the attachment kind.
6. Send a prompt and verify streamed assistant output, agent activity, tool activity, and proposal events render in the workspace.
7. Stop the backend or use a backend without the AI feature and verify Weaver shows an unavailable state rather than throwing.

## Validation Commands

```bash
dotnet test src/modules/Elsa.Studio.AI.Tests/Elsa.Studio.AI.Tests.csproj
dotnet build src/hosts/Elsa.Studio.Host.Server/Elsa.Studio.Host.Server.csproj --no-restore
dotnet build src/hosts/Elsa.Studio.Host.Wasm/Elsa.Studio.Host.Wasm.csproj --no-restore
```
