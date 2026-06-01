# Persistence vNext Workflow Runtime Decision

Issue: #7618

## Decision

Do not migrate workflow runtime hot paths to Persistence vNext by default.

Persistence vNext is approved only for workflow management/catalog/metadata-style persistence prototypes where queries are declared-index backed and do not require full workflow-state blob reads. Runtime execution hot paths remain on specialized stores until provider-backed physicalization benchmarks prove equivalent behavior.

## Evidence

The decision is based on the #7617 benchmark baseline in [Persistence vNext Workflow Runtime Benchmarks](persistence-vnext-workflow-runtime-benchmarks.md).

Key observations:

- Workflow instance status/executing query: 458.525 us mean.
- Workflow instance optimistic update: 28.377 us mean in the synthetic runtime entity document path.
- Bookmark lookup by stimulus and workflow instance: 7,679.100 us mean.
- Trigger lookup by stimulus hash: 7,625.610 us mean.
- Activity log query by workflow instance: 9,968.678 us mean.
- Synthetic document lease create/delete: 6.566 us mean, but without distributed lock contention, lease expiry, or cross-node semantics.
- Workflow log append: 5.415 us mean per operation in the synthetic path, but without real provider batch insert, retention, or indexed paging pressure.

The benchmark helper intentionally uses an in-memory document store that scans and parses JSON. That makes it a conservative candidate baseline rather than a provider throughput claim. The result is still useful: runtime queries that depend on bookmark, trigger, or log lookup shapes are not acceptable migration targets until concrete providers demonstrate true index-backed query behavior.

## Approved For vNext Prototyping

Approved candidates:

- Workflow instance management metadata and summary queries when implemented as declared-index-backed documents.
- Workflow instance ID projections and counts that filter only on indexed metadata fields.
- Future management/catalog-style stores that do not participate in per-dispatch, per-resume, per-activity, or per-log hot loops.

Conditions:

- The prototype must preserve current public API behavior.
- The prototype must not replace runtime execution stores.
- The prototype must benchmark small and large workflow-state payloads separately before any state-blob write path is approved.
- The prototype must keep the existing EF Core persistence path side-by-side until a migration story exists.

## Remain Specialized

The following stores remain specialized:

- `IBookmarkStore`
- `ITriggerStore`
- `IBookmarkQueueStore`
- `IBookmarkQueueDeadLetterStore`
- `IWorkflowExecutionLogStore`
- `IActivityExecutionStore`
- Runtime distributed lock providers and queue worker coordination

Reasons:

- They are per-dispatch, per-resume, per-commit, or high-volume execution paths.
- They require predictable index behavior, batch writes, ordered paging, idempotency, and concurrency semantics.
- Locking needs explicit distributed lease/lock behavior, not accidental document-store uniqueness.
- The benchmark baseline shows lookup/query shapes are not acceptable without provider physicalization.

## Follow-Up Implementation

Only one follow-up implementation path is approved now:

- Prototype workflow instance metadata persistence on Persistence vNext, limited to indexed management/catalog queries and summary projections: #7662.

No follow-up implementation issue should be created for bookmark, trigger, queue, runtime log, activity log, or lock migration until provider-specific benchmarks show acceptable hot-path behavior.

## No-Premature-Migration Rule

Workflow runtime hot paths must stay on their current specialized persistence providers unless a future decision record includes:

- Provider-specific benchmark results for the exact store and query shape.
- Correctness coverage for concurrency, idempotency, ordering, and tenant isolation.
- A rollback plan that preserves existing EF Core/runtime provider behavior.
- Explicit approval naming the store being migrated.

This rule applies even if the store is technically document-shaped. Runtime frequency and concurrency semantics take precedence over shape alone.
