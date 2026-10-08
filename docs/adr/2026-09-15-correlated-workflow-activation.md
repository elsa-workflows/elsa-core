# Refuse duplicate running instances through activation strategies

Date: 2026-09-15

## Status

Accepted

## Context

Dispatching or starting a workflow with a `CorrelationId` could create two Running instances for the same conversation. Callers that looked up an existing instance and then called `DispatchAsync` hit a check-then-act race, and the dispatch path never evaluated `IWorkflowActivationStrategy`. A unique index on `WorkflowInstance.CorrelationId` was suggested as the database-level fix.

`CorrelationId` is a routing and grouping key, not a global identity:

- The default (no strategy / `AllowAlwaysStrategy`) allows many Running instances to share a correlation ID. `BulkDispatchWorkflows` can assign the same correlation to many children of one definition.
- `SingletonStrategy` allows at most one Running instance per `(TenantId, DefinitionId)`.
- `CorrelatedSingletonStrategy` allows at most one Running instance per `(TenantId, DefinitionId, CorrelationId)`.
- `CorrelationStrategy` allows at most one Running instance per `(TenantId, CorrelationId)` across definitions.
- Blank or missing correlation IDs remain unbounded when no correlation-based strategy is selected. The two correlation-based strategies require a non-blank correlation ID and fail explicitly when it is missing. A unique index cannot express those scopes and would reject legitimate multi-instance use.

The existing strategies also treated a blank `CorrelationId` as "match every Running instance", which accidentally made them behave like a singleton.

## Decision

Do not add a unique index on `CorrelationId`.

Honor the workflow's activation strategy on every create path, including `DispatchWorkflowDefinition` → `CreateAndRunInstanceAsync`. Built-in singleton strategies acquire a distributed lock whose name is derived from a length-prefixed, canonical hash of the tenant and strategy scope; raw tenant and correlation values are not included in the lock name or denial log. A second create is refused (`CannotStart`); it does not attach to or resume the existing instance. Resume remains the stimulus / bookmark path.

Create-only requests persist under the activation lease. Create-and-run requests keep the candidate instance in memory until the runner commits its first resulting state, and hold the activation lease through that boundary; a failure or cancellation before commit must not leave a provisional Running/Pending row. Finished instances do not occupy the slot. The tradeoff is that synchronous nested dispatch which reuses the parent's built-in uniqueness scope can wait for the parent-held lease; workflows that synchronously await a nested activation should use distinct correlation scopes.

Custom `IWorkflowActivationStrategy` implementations remain source-compatible and are still evaluated, but the runtime does not guess their exclusivity scope. They are not made atomic across concurrent callers unless they adopt a future explicit scope capability.

## Consequences

Conversation workflows that want one Running instance per correlation ID must set `CorrelatedSingletonStrategy` (or `CorrelationStrategy` for a process-wide conversation key) and provide a non-blank correlation ID. Dispatch and start enforce built-in scopes under concurrency, including multi-node hosts with a cross-node lock provider. A configured but unregistered strategy fails closed with an actionable configuration error.

Default grouping behavior is unchanged. Hosts that used `CorrelationId` only as a tag continue to create multiple Running instances.

A unique filtered index could still be added later as an extra backstop for a single chosen scope; it is not the product rule.
