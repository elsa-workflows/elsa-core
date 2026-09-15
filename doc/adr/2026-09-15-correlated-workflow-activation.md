# Refuse duplicate running instances through activation strategies

Date: 2026-09-15

## Status

Accepted

## Context

Dispatching or starting a workflow with a `CorrelationId` could create two Running instances for the same conversation. Callers that looked up an existing instance and then called `DispatchAsync` hit a check-then-act race, and the dispatch path never evaluated `IWorkflowActivationStrategy`. A unique index on `WorkflowInstance.CorrelationId` was suggested as the database-level fix.

`CorrelationId` is a routing and grouping key, not a global identity:

- The default (no strategy / `AllowAlwaysStrategy`) allows many Running instances to share a correlation ID. `BulkDispatchWorkflows` can assign the same correlation to many children of one definition.
- `CorrelatedSingletonStrategy` allows at most one Running instance per `(DefinitionId, CorrelationId)`.
- `CorrelationStrategy` allows at most one Running instance per `CorrelationId` across definitions.
- Blank or missing correlation IDs must remain unbounded. A unique index cannot express those three scopes and would reject legitimate multi-instance use.

The existing strategies also treated a blank `CorrelationId` as "match every Running instance", which accidentally made them behave like a singleton.

## Decision

Do not add a unique index on `CorrelationId`.

Honor the workflow's activation strategy on every create path, including `DispatchWorkflowDefinition` → `CreateAndRunInstanceAsync`. Serialize the strategy check with the persist of the new instance using the existing distributed lock provider, keyed by the strategy's uniqueness scope. A second create is refused (`CannotStart`); it does not attach to or resume the existing instance. Resume remains the stimulus / bookmark path.

Blank correlation IDs skip correlation-scoped uniqueness. Finished instances do not occupy the slot.

## Consequences

Conversation workflows that want one Running instance per correlation ID must set `CorrelatedSingletonStrategy` (or `CorrelationStrategy` for a process-wide conversation key). Dispatch and start now enforce that under concurrency, including multi-node hosts with a cross-node lock provider.

Default grouping behavior is unchanged. Hosts that used `CorrelationId` only as a tag continue to create multiple Running instances.

A unique filtered index could still be added later as an extra backstop for a single chosen scope; it is not the product rule.
