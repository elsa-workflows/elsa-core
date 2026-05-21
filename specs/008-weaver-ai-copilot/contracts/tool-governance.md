# Tool Governance Contract: Weaver AI Copilot Platform

## Required Tool Metadata

Every tool must declare:

- `Name`: stable namespaced identifier.
- `Description`: concise behavior description.
- `Schema`: structured input schema.
- `Mutability`: `ReadOnly`, `Proposal`, or `Administrative`.
- `DangerLevel`: `Low`, `Medium`, `High`, or `Critical`.
- `Permissions`: required Elsa permissions.
- `TenantBehavior`: `TenantScoped`, `HostScoped`, or `CrossTenantDenied`.
- `AuditBehavior`: `None`, `RecordInvocation`, or `RecordInvocationAndResult`.
- `AllowedAgents`: optional agent allowlist.

## Registration APIs

```csharp
public static class AiFeatureExtensions
{
    public static AiFeature AddAiTool<TTool>(this AiFeature feature) where TTool : class, IAiTool;
    public static AiFeature AddAiContextProvider<TProvider>(this AiFeature feature) where TProvider : class, IAiContextProvider;
    public static AiFeature AddAiAgent<TAgent>(this AiFeature feature) where TAgent : class, IAiAgentDefinitionProvider;
    public static AiFeature AddMcpServer(this AiFeature feature, Action<AiMcpServerOptions> configure);
}
```

## Enforcement Order

1. Resolve current actor and tenant.
2. Resolve agent scope.
3. Find tool definition.
4. Validate tool is enabled globally and for the agent.
5. Validate tenant behavior.
6. Validate permissions and ownership.
7. Validate mutability and danger policy.
8. Redact arguments.
9. Execute tool.
10. Redact result.
11. Emit stream and audit events.

## Mutability Policy

- `ReadOnly`: may inspect authorized Elsa data but cannot create proposals or mutate state.
- `Proposal`: may create or validate proposals but cannot directly apply them.
- `Administrative`: reserved for future work and disabled in MVP.

## Enablement Policy

- Read-only tools registered by enabled modules may be enabled by default.
- Proposal tools require explicit administrator enablement.
- Administrative tools are disabled in MVP even if registered.
- MCP-backed tools require explicit administrator enablement and allowlisted tool names.

## MCP Policy

- MCP servers are configured server-side.
- Only allowlisted MCP tools are exposed.
- MCP tools inherit the same metadata and enforcement path as built-in tools.
- Per-agent MCP scoping is required.
- MCP tools are unavailable until explicitly enabled by an administrator.
