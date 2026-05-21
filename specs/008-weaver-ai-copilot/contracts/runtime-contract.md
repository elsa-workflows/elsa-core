# Runtime Contract: Weaver AI Copilot Platform

## Core Abstractions

```csharp
public interface IAiProvider
{
    ValueTask<AiSessionHandle> CreateSessionAsync(CreateAiSessionRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AiProviderEvent> ExecuteTurnAsync(AiTurnRequest request, CancellationToken cancellationToken = default);
}

public interface IAiOrchestrator
{
    IAsyncEnumerable<AiStreamEvent> ExecuteChatAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}

public interface IAiTool
{
    AiToolDefinition Definition { get; }
    ValueTask<AiToolResult> ExecuteAsync(AiToolExecutionContext context, CancellationToken cancellationToken = default);
}

public interface IAiToolRegistry
{
    ValueTask<IReadOnlyCollection<AiToolDefinition>> ListAsync(AiToolQuery query, CancellationToken cancellationToken = default);
    ValueTask<IAiTool?> FindAsync(string name, CancellationToken cancellationToken = default);
}

public interface IAiContextProvider
{
    string Kind { get; }
    ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default);
}

public interface IAiProposalStore
{
    ValueTask<AiProposal?> FindAsync(string id, CancellationToken cancellationToken = default);
    ValueTask SaveAsync(AiProposal proposal, CancellationToken cancellationToken = default);
}

public interface IAiAuditSink
{
    ValueTask RecordAsync(AiAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
```

`IAiProposalStore` and `IAiAuditSink` require durable production implementations for MVP. Conversation/session retention remains configurable and may use in-memory storage for development and tests.

## Provider Boundary

- `Elsa.AI.Abstractions` owns `AiProviderEvent`, `AiStreamEvent`, session, tool, context, proposal, and audit models.
- `Elsa.AI.Copilot` maps Copilot SDK and CLI events into Elsa-owned models.
- No Copilot SDK type may appear in `Elsa.AI.Abstractions`, `Elsa.AI.Host`, workflow models, REST contracts, or Studio contracts.

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
- Record success, failure, and denial through `IAiAuditSink`.

## Runtime Scope Rules

- In-progress chat turns continue for a configurable disconnect grace window and expose recoverable durable outputs to authorized reconnecting clients.
- Runtime trend analysis is limited by default to attached references plus an explicit time range and diagnostics scope.
- The same authorized user may request, approve, reject, and apply proposals in MVP.
