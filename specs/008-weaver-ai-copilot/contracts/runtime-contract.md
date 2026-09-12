# Runtime Contract: Weaver AI Copilot Platform

## Core Abstractions

```csharp
public interface IAIProvider
{
    string Name { get; }
    ValueTask<AISessionHandle> CreateSessionAsync(CreateAISessionRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AIProviderEvent> ExecuteTurnAsync(AITurnRequest request, IAIProviderToolInvoker toolInvoker, CancellationToken cancellationToken = default);
}

public interface IAIOrchestrator
{
    IAsyncEnumerable<AIStreamEvent> ExecuteChatAsync(AIChatRequest request, CancellationToken cancellationToken = default);
}

public interface IAITool
{
    AIToolDefinition Definition { get; }
    ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default);
}

public interface IAIToolRegistry
{
    ValueTask<IReadOnlyCollection<AIToolDefinition>> ListAsync(AIToolQuery query, CancellationToken cancellationToken = default);
    ValueTask<IAITool?> FindAsync(string name, AIToolQuery query, CancellationToken cancellationToken = default);
}

public interface IAIProviderToolInvoker
{
    ValueTask<AIToolResult> InvokeAsync(AIProviderToolInvocation invocation, CancellationToken cancellationToken = default);
}

public interface IAIContextProvider
{
    string Kind { get; }
    ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default);
}

public interface IAIProposalStore
{
    ValueTask<AIProposal?> FindAsync(string id, string? tenantId, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(AIProposal proposal, CancellationToken cancellationToken = default);
}

public interface IAIAuditSink
{
    ValueTask RecordAsync(AIAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
```

`IAIProposalStore` and `IAIAuditSink` require durable production implementations for MVP. Conversation/session retention remains configurable and may use in-memory storage for development and tests.

## Provider Boundary

- `Elsa.AI.Abstractions` owns `AIProviderEvent`, `AIStreamEvent`, session, tool, context, proposal, and audit models.
- `Elsa.AI.Host` resolves context and the authorized tool set, but does not run a model/tool continuation loop.
- `Elsa.AI.Copilot` uses `GitHub.Copilot.SDK` sessions directly, registers Elsa tools as governed Copilot SDK callbacks, and maps Copilot SDK events into Elsa-owned models.
- No Copilot SDK type may appear in `Elsa.AI.Abstractions`, `Elsa.AI.Host`, workflow models, REST contracts, or Studio contracts.

## Agent Loop Ownership

- Copilot SDK owns agent planning, tool invocation sequencing, continuation turns, custom agent selection, MCP wiring, permission callbacks, hooks, and provider session state.
- Elsa Host owns tenant-safe context attachment resolution, RBAC-filtered tool metadata, server-side tool execution, proposal-only mutation enforcement, audit, redaction, and Studio stream contracts.
- Provider adapters may expose provider-specific features through provider configuration metadata, but those details remain inside the adapter module and are mapped to Elsa-owned contracts before reaching Studio.

## Built-In MVP Tools

| Tool | Mutability | Purpose |
|------|------------|---------|
| `workflow.getDefinition` | Read-only | Retrieve an authorized workflow definition summary and graph payload. |
| `workflow.listDefinitions` | Read-only | Search authorized workflow definitions. |
| `instance.get` | Read-only | Retrieve an authorized workflow instance summary and state. |
| `instance.search` | Read-only | Search authorized workflow instances by status, workflow, and time range. |
| `activities.catalog` | Read-only | Retrieve activity descriptors available to the tenant. |
| `workflow.proposeCreate` | Proposal | Create a workflow proposal from structured draft output. |
| `workflow.proposeUpdate` | Proposal | Create an update proposal from a baseline and structured patch. |
| `workflow.validateDraft` | Proposal | Validate a structured workflow draft without persisting it. |

## Tool Execution Rules

- Validate tenant, RBAC, ownership, danger level, global enablement, and agent scope before execution.
- Resolve persistence through Elsa service abstractions only.
- Redact arguments and results before stream and audit output.
- Clamp result size before model context inclusion.
- Record success, failure, and denial through `IAIAuditSink`.

## Runtime Scope Rules

- In-progress chat turns continue for a configurable disconnect grace window and expose recoverable durable outputs to authorized reconnecting clients.
- Runtime trend analysis is limited by default to attached references plus an explicit time range and diagnostics scope.
- The same authorized user may request, approve, reject, and apply proposals in MVP.
