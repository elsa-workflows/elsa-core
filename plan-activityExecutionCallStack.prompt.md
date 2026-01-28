# Plan: Activity Execution Call Stack Implementation (Hybrid: explicit + ambient)

This plan implements a comprehensive call stack mechanism to track the execution chain from root workflow through all parent activities to a specific activity execution, enabling visibility into the complete invocation hierarchy when viewing activity execution records.

Core design
- Explicit predecessor: Activities that know the causal predecessor (e.g., a completed child that schedules the next) set `SchedulingActivityExecutionId` directly via scheduling options.
- Ambient fallback: During completion callbacks, bookmark resumes, and child-workflow starts, the workflow sets an ambient "current scheduling source" on the `WorkflowExecutionContext`. If a schedule call omits `SchedulingActivityExecutionId`, the scheduler fills it from the ambient. This minimizes code churn while preserving correctness.
- Structural vs temporal: Keep `Owner`/`ParentActivityExecutionContext` for structural containment; use `SchedulingActivityExecutionId`/`SchedulingWorkflowInstanceId` for the temporal execution chain.

## Steps

### 1. Add call stack fields to scheduling models throughout the chain

Add `SchedulingActivityExecutionId` (nullable `string`) to:
- [`ScheduleWorkOptions`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Options/ScheduleWorkOptions.cs)
- [`ScheduledActivityOptions`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Models/ScheduledActivityOptions.cs)
- [`ActivityWorkItem`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Models/ActivityWorkItem.cs)
- [`ActivityInvocationOptions`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Options/ActivityInvocationOptions.cs)

Include clear XML documentation explaining this tracks the temporal/execution predecessor (distinct from structural `Owner`/`ParentActivityExecutionContext`).

Update all constructors and property mappings in:
- [`WorkflowExecutionContextSchedulerStrategy.Schedule`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Services/WorkflowExecutionContextSchedulerStrategy.cs)
- [`DefaultActivitySchedulerMiddleware.ExecuteWorkItemAsync`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Middleware/Workflows/DefaultActivitySchedulerMiddleware.cs)

Thread this value through the scheduling chain.

### 2. Store call stack fields in runtime and persisted contexts

Add the following fields to [`ActivityExecutionContext`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs):
- `SchedulingActivityExecutionId` (nullable `string`)
- `SchedulingActivityId` (nullable `string` - denormalized for convenience)
- `SchedulingWorkflowInstanceId` (nullable `string` - for cross-workflow tracking)

Include XML comments distinguishing these from `ParentActivityExecutionContext`:
- **`ParentActivityExecutionContext`**: The structural container activity (e.g., Flowchart contains all its children). Represents the hierarchical parent in the workflow structure.
- **`SchedulingActivityExecutionId`**: The temporal/execution predecessor that directly triggered execution of this activity. Tracks the execution sequence, not the structural hierarchy.
- **`SchedulingWorkflowInstanceId`**: The workflow instance ID of the activity that invoked this activity's workflow. Set when crossing workflow boundaries (e.g., via `ExecuteWorkflow` or `DispatchWorkflow`).

Update [`WorkflowExecutionContext.CreateActivityExecutionContextAsync`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Contexts/WorkflowExecutionContext.cs) to accept and store these fields from `ActivityInvocationOptions`.

Add corresponding fields to [`ActivityExecutionRecord`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Runtime/Entities/ActivityExecutionRecord.cs):
- `SchedulingActivityExecutionId` (nullable `string`)
- `SchedulingActivityId` (nullable `string`)
- `SchedulingWorkflowInstanceId` (nullable `string`)
- `CallStackDepth` (nullable `int`)

Update [`DefaultActivityExecutionMapper.MapAsync`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Runtime/Services/DefaultActivityExecutionMapper.cs) to populate these fields from the execution context. Calculate `CallStackDepth` by traversing the `SchedulingActivityExecutionId` chain until reaching null.

### 3. Add ambient scheduling source to the workflow context

Add an ambient scheduling source to [`WorkflowExecutionContext`]:
- Fields (transient): `CurrentSchedulingActivityExecutionId`, `CurrentSchedulingWorkflowInstanceId`.
- API: `IDisposable BeginSchedulingScope(string? activityExecutionId, string? workflowInstanceId)` that pushes values and restores previous values on dispose.

Set ambient scope in these places:
- Around owner completion callbacks (where next activities are scheduled).
- Around bookmark-resume handlers (background completions resuming the workflow).
- At child-workflow start (root activity creation).

Modify [`WorkflowExecutionContextSchedulerStrategy.Schedule`] to set `SchedulingActivityExecutionId` and `SchedulingWorkflowInstanceId` from `ScheduleWorkOptions` if provided; otherwise fall back to the ambient `WorkflowExecutionContext` values.

### 4. Update composite activities to capture scheduling activity context

In [`Flowchart.Counters.cs`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Activities/Flowchart/Activities/Flowchart.Counters.cs):
- Update `ScheduleOutboundActivityAsync` to populate `ScheduleWorkOptions.SchedulingActivityExecutionId = completedActivityContext.Id` when a completed activity schedules its outbound activities.
- Update `MaybeScheduleBackwardConnectionActivityAsync` to include `SchedulingActivityExecutionId` in the `ScheduleWorkOptions`.
- Update `MaybeScheduleWaitAllActivityAsync`, `MaybeScheduleWaitAllActiveActivityAsync`, `MaybeScheduleWaitAnyActivityAsync` to pass `SchedulingActivityExecutionId` when scheduling.

In [`Flowchart.Tokens.cs`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Core/Activities/Flowchart/Activities/Flowchart.Tokens.cs):
- Update `OnChildCompletedTokenBasedLogicAsync` to pass `SchedulingActivityExecutionId` in `ScheduleWorkOptions` when scheduling subsequent activities.

Apply the same pattern to other composite activities:
- `Sequence`
- `ForEach`
- `Parallel`
- `While`
- `Do`
- Any other activities that schedule child activities based on completion

When scheduling a child activity that was directly triggered by another activity's completion, set `SchedulingActivityExecutionId` to the completing activity's execution context ID. When omitted, the ambient scope ensures a sensible fallback.

### 5. Handle cross-workflow call stack linkage (span by default)

For [`ExecuteWorkflow`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Runtime/Activities/ExecuteWorkflow.cs) and [`DispatchWorkflow`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Runtime/Activities/DispatchWorkflow.cs):

- Capture the calling activity's `ExecutionId` and current `WorkflowInstanceId`.
- Pass them through `RunWorkflowOptions` and `DispatchWorkflowRequest` to the child workflow.
- When the child workflow starts, set the first activity's:
  - `SchedulingActivityExecutionId` to the parent's invocation activity's execution ID.
  - `SchedulingWorkflowInstanceId` to the parent workflow instance ID.
- Also set the ambient scope (`BeginSchedulingScope`) for the duration of child start so subsequent schedules inherit these values by default.

Cross-workflow chains should be considered part of the call stack by default (span by default).

### 6. Add call stack depth and create database migration

Add `CallStackDepth` (nullable `int`) field to [`ActivityExecutionRecord`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Runtime/Entities/ActivityExecutionRecord.cs).

In [`DefaultActivityExecutionMapper.MapAsync`](file:///Users/sipke/Projects/Elsa/elsa-core/main/src/modules/Elsa.Workflows.Runtime/Services/DefaultActivityExecutionMapper.cs):
- Calculate `CallStackDepth` by traversing the source `ActivityExecutionContext.SchedulingActivityExecutionId` chain until reaching null.
- Use root depth = 0 (documented convention).
- Store this value in the `ActivityExecutionRecord`.

Create EF Core migration adding indexed columns to `ActivityExecutionRecord` table:
- `SchedulingActivityExecutionId` (indexed, nullable)
- `SchedulingActivityId` (indexed, nullable)
- `SchedulingWorkflowInstanceId` (indexed, nullable)
- `CallStackDepth` (indexed, nullable)

The `CallStackDepth` index enables efficient filtering by execution depth without reconstructing the full chain (e.g., "show me all activities at depth > 5").

### 7. Implement call stack query and reconstruction APIs

Implement `IActivityExecutionStore.GetExecutionChainAsync(string activityExecutionId, bool includeCrossWorkflowChain = true, int? skip = null, int? take = null)` that:
- Recursively queries `SchedulingActivityExecutionId` until reaching root (null).
- Follows `SchedulingWorkflowInstanceId` across workflow boundaries by default (span by default). Optionally allow disabling cross-workflow span via parameter.
- Supports pagination via `skip` and `take` parameters to handle deep call stacks efficiently.
- Returns a paginated result containing:
  - `Items`: List of execution records (ordered from root to current activity, or subset if paginated)
  - `TotalCount`: Total number of items in the full chain
  - `Skip`: The skip value used
  - `Take`: The take value used
- When pagination is not specified (`skip` and `take` are null), returns the full chain.

Add extension methods:
- **`ActivityExecutionContext.GetExecutionChain(int? skip = null, int? take = null)`**: Reconstruct the runtime call stack by traversing `SchedulingActivityExecutionId`, returning a paginated result from root to current activity.
- **`ActivityExecutionRecord.GetExecutionChainAsync(IActivityExecutionStore, bool includeCrossWorkflowChain = true, int? skip = null, int? take = null)`**: Reconstruct persisted call stacks by querying the store with pagination support.

Both methods should return results ordered from root to current activity, with pagination applied after ordering.

Add REST API endpoint:
- **`GET /api/workflow-instances/{workflowInstanceId}/activity-executions/{activityExecutionId}/call-chain`**
  - Query parameters:
    - `includeCrossWorkflowChain` (bool, default: true): Include parent workflow activities across workflow boundaries
    - `skip` (int?, optional): Number of items to skip (for pagination)
    - `take` (int?, optional): Number of items to return (for pagination, recommended max: 100)
  - Response:
    - `items`: Array of activity execution records
    - `totalCount`: Total number of items in the full chain
    - `skip`: The skip value used
    - `take`: The take value used (or null if full chain returned)
  - This enables UI to implement paginated/lazy loading for deep call stacks.

## Further Considerations

### 1. Structural vs temporal hierarchy documentation

`ParentActivityExecutionContext` represents the structural container (e.g., Flowchart contains all its children), while `SchedulingActivityExecutionId` tracks the temporal execution predecessor (e.g., Activity B completed and directly triggered Activity C).

These are orthogonal relationships:
- A Flowchart can own many children (structural), but only a predecessor directly triggers the next (temporal).
- When Activity B completes, it schedules the next child, establishing a temporal link via `SchedulingActivityExecutionId`.

All XML comments for these fields should explicitly clarify this distinction to prevent developer confusion and misuse.

### 2. Ambient scope guardrails

- The ambient scope must be short-lived and always disposed via `using`/`finally` to avoid leakage between unrelated scheduling operations.
- The scheduler should prefer explicit `ScheduleWorkOptions.SchedulingActivityExecutionId`/`SchedulingWorkflowInstanceId` and only fall back to ambient when not provided.
- Document that the ambient exists to reduce boilerplate and should not be relied upon when explicit causal context is readily available.

### 3. Cross-workflow boundary reconstruction (default span)

`SchedulingWorkflowInstanceId` enables reconstructing call stacks that span multiple workflow instances:
- Parent Workflow (Instance A) → ExecuteWorkflow activity (in Instance A) → Child Workflow (Instance B) → failing activity (in Instance B).

Since span is the default, cross-instance traversal should occur unless explicitly disabled.

### 4. Call stack depth optimization trade-offs

Storing `CallStackDepth` trades a small amount of storage for simpler, faster queries and analytics:

**Benefits:**
- Efficient filtering by depth ranges (e.g., "depth > 5").
- Early termination in chain reconstruction.
- Index-based analytics queries.
- Lower storage than persisting full chains.

**Drawbacks:**
- `CallStackDepth` becomes stale if parent records are deleted or altered post-hoc. Prefer immutable execution records.
- Document that `CallStackDepth` is an optimization hint for querying, not an authoritative source if retention policies prune ancestors.

### 5. Testing matrix

- Sequential flow: A → B → C (explicit predecessor set, ambient unused).
- Parallel fan-out: A schedules B and C (both record A as predecessor; ambient vs explicit).
- Nested composites: Multiple owners scheduling into the same queue.
- Background resume: Bookmark-based resumes interleaving with other work; ambient set during resume.
- Cross-workflow: Execute/Dispatch child workflow; default spanning chain.
- Deduplication scenarios: `PreventDuplicateScheduling` and re-scheduling.
- Persistence/round-trips: Background/persisted scheduled activities using `ScheduledActivityOptions`.
- Deep call stacks: Test pagination with chains deeper than 100 activities.
- Cross-workflow pagination: Ensure pagination works correctly when spanning workflow boundaries.

### 6. Performance and pagination considerations

**Deep call stack handling:**
- For very deep call stacks (e.g., recursive workflows or long-running sequential processes), retrieving the entire chain in a single query can be expensive.
- Pagination (`skip`/`take`) enables efficient loading in the UI with incremental/lazy loading patterns.
- Recommend default `take` of 50-100 items per page for REST API calls.
- The `CallStackDepth` field enables quick assessment of chain depth before deciding whether to paginate.

**Query optimization strategies:**
- Use indexed lookups on `SchedulingActivityExecutionId` to traverse the chain efficiently.
- Consider caching strategies for frequently accessed chains (e.g., recently failed activities).
- For cross-workflow queries, implement efficient join strategies or batched lookups to minimize round-trips.
- Document that pagination is "forward-only" (skip/take from root toward current) to align with typical debugging workflows (start at root, drill down).

**REST API rate limiting:**
- Consider rate limiting on the call chain endpoint if it becomes a performance bottleneck.
- Monitor query performance and adjust default pagination sizes based on observed data.
