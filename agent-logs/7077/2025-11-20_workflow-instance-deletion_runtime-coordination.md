# Workflow Instance Deletion: Runtime Coordination and Separation of Concerns

**Date**: 2025-11-20
**Branch**: bug/7077
**Commit**: 1f326295a38eee236b84d60e69afdf9e3a1eda1a

---

## Problem Statement

Workflow instances were not being properly deleted when using the Delete and BulkDelete API endpoints. The deletion operations were failing silently or inconsistently, particularly for running workflow instances.

## Root Cause

The deletion endpoints were directly using `IWorkflowInstanceManager` to delete workflow instances from the database without proper coordination with the workflow runtime. This caused several issues:

1. **No coordination for running workflows**: Running workflows need to be properly stopped/cancelled before deletion
2. **Race conditions**: In distributed scenarios, concurrent operations could cause database inconsistencies
3. **Missing cleanup**: Runtime state, bookmarks, and other related data weren't being cleaned up properly
4. **Violation of separation of concerns**: API layer was directly manipulating persistence layer without going through the runtime

## Solution Architecture

The solution refactors the deletion logic to properly use the workflow runtime infrastructure, ensuring proper coordination and cleanup.

### Key Design Principles

1. **Runtime coordination**: All workflow instance operations should go through `IWorkflowRuntime` and `IWorkflowClient`
2. **Proper cancellation**: Running workflows must be cancelled before deletion
3. **Distributed locking**: Use the same distributed locks as execution to prevent concurrent modifications
4. **Separation by status**: Handle running and finished instances differently

### Implementation Details

#### 1. Added Delete Method to IWorkflowClient

**File**: `src/modules/Elsa.Workflows.Runtime/Contracts/IWorkflowClient.cs`

```csharp
/// <summary>
/// Deletes the workflow instance. This method ensures that the instance is properly stopped before deletion.
/// </summary>
Task<bool> DeleteAsync(CancellationToken cancellationToken = default);
```

**Rationale**: The `IWorkflowClient` is the proper abstraction for workflow instance operations. Adding deletion here ensures:
- Consistent API surface
- Proper coordination with other operations
- Support for both local and distributed scenarios

#### 2. Implemented LocalWorkflowClient.DeleteAsync

**File**: `src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs`

**Key logic**:
```csharp
public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
{
    // Load the workflow instance (single DB call)
    var workflowInstance = await TryGetWorkflowInstanceAsync(cancellationToken);
    if (workflowInstance == null)
        return false;

    // Cancel if running (ensures proper cleanup)
    await CancelAsync(workflowInstance, cancellationToken);

    // Delete the workflow instance
    var filter = new WorkflowInstanceFilter { Id = workflowInstanceId };
    await workflowInstanceManager.DeleteAsync(filter, cancellationToken);
    return true;
}
```

**Key features**:
- **Single DB read**: Loads the instance once and reuses it
- **Proper cancellation**: Ensures running workflows are stopped first
- **Graceful handling**: Returns `false` if instance doesn't exist
- **Clean deletion**: Uses the manager's delete method for proper cleanup

**Supporting refactoring**:
- Extracted `CancelAsync(WorkflowInstance)` private method to avoid loading the instance twice
- Added `TryGetWorkflowInstanceAsync()` helper for non-throwing retrieval

#### 3. Implemented DistributedWorkflowClient.DeleteAsync

**File**: `src/modules/Elsa.Workflows.Runtime.Distributed/Services/DistributedWorkflowClient.cs`

```csharp
public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
{
    // Use the same distributed lock as for execution to prevent concurrent DB writes
    return await WithLockAsync(async () => await _localWorkflowClient.DeleteAsync(cancellationToken));
}
```

**Key feature**: Uses the same distributed lock mechanism (`workflow-instance:{id}`) as execution operations, preventing:
- Concurrent modifications during deletion
- Race conditions between delete and execute operations
- Inconsistent state in distributed scenarios

#### 4. Refactored Single Instance Delete Endpoint

**File**: `src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances/Delete/Endpoint.cs`

**Before**:
```csharp
internal class Delete(IWorkflowInstanceManager store) : ElsaEndpoint<Request>
{
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var filter = new WorkflowInstanceFilter { Id = request.Id };
        var deleted = await store.DeleteAsync(filter, cancellationToken);
        // ...
    }
}
```

**After**:
```csharp
internal class Delete(IWorkflowRuntime workflowRuntime) : ElsaEndpoint<Request>
{
    public override async Task HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var client = await workflowRuntime.CreateClientAsync(request.Id, cancellationToken);
        var deleted = await client.DeleteAsync(cancellationToken);
        // ...
    }
}
```

**Changes**:
- ✅ Uses `IWorkflowRuntime` instead of `IWorkflowInstanceManager`
- ✅ Creates a workflow client for proper coordination
- ✅ Relies on runtime infrastructure for proper deletion

#### 5. Refactored Bulk Delete Endpoint

**File**: `src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances/BulkDelete/Endpoint.cs`

**Strategy**: Split deletion into two phases based on workflow status:

**Phase 1: Delete Running Instances Individually**
```csharp
// Step 1: Delete running instances individually (requires coordination by the workflow runtime).
var runningFilter = new WorkflowInstanceFilter
{
    Ids = baseFilter.Ids,
    DefinitionId = baseFilter.DefinitionId,
    DefinitionIds = baseFilter.DefinitionIds,
    WorkflowStatus = WorkflowStatus.Running
};

var runningInstanceIds = await workflowInstanceStore.FindManyIdsAsync(runningFilter, cancellationToken);
var count = 0L;

foreach (var instanceId in runningInstanceIds)
{
    var client = await workflowRuntime.CreateClientAsync(instanceId, cancellationToken);
    var deleted = await client.DeleteAsync(cancellationToken);
    if (deleted)
        count++;
}
```

**Phase 2: Bulk Delete Finished Instances**
```csharp
// Step 2: Bulk delete finished instances (no coordination needed).
var finishedFilter = new WorkflowInstanceFilter
{
    Ids = baseFilter.Ids,
    DefinitionId = baseFilter.DefinitionId,
    DefinitionIds = baseFilter.DefinitionIds,
    WorkflowStatus = WorkflowStatus.Finished
};

var finishedDeletedCount = await workflowInstanceStore.DeleteAsync(finishedFilter, cancellationToken);
count += finishedDeletedCount;
```

**Rationale**:
- **Running instances**: Need individual coordination through the runtime (cancellation, locks, cleanup)
- **Finished instances**: Can be safely bulk-deleted as they have no active runtime state
- **Performance**: Avoids unnecessary overhead for finished workflows while ensuring safety for running ones
- **Correctness**: Each running workflow gets proper cancellation and distributed locking

**Trade-offs**:
- ✅ **Correctness**: Ensures proper cleanup and coordination
- ✅ **Safety**: Prevents race conditions and inconsistencies
- ⚠️ **Performance**: Running instances are deleted one-by-one instead of bulk
  - This is acceptable because:
    - Running instances are typically a small percentage of total instances
    - Proper coordination is more important than bulk performance
    - Finished instances still use efficient bulk deletion

## Benefits

### 1. Proper Coordination
- Running workflows are properly cancelled before deletion
- Runtime state is cleaned up correctly
- Bookmarks and related data are removed

### 2. Distributed Safety
- Uses the same distributed locks as execution
- Prevents race conditions in multi-node deployments
- Ensures consistency across the cluster

### 3. Separation of Concerns
- API layer uses runtime abstractions
- No direct persistence manipulation
- Easier to maintain and extend

### 4. Consistent Behavior
- Same deletion logic for single and bulk operations
- Predictable behavior across all scenarios
- Proper error handling and return values

## Testing Verification

### Manual Testing
- ✅ Delete single running workflow instance
- ✅ Delete single finished workflow instance
- ✅ Delete non-existent workflow instance (returns false)
- ✅ Bulk delete mixed running and finished instances
- ✅ Bulk delete by definition ID
- ✅ Verify proper cancellation before deletion
- ✅ Verify distributed locking in multi-node scenario

### Edge Cases Handled
- **Non-existent instance**: Returns `false` gracefully
- **Already cancelled/finished**: No-op on cancellation, proceeds to delete
- **Concurrent operations**: Distributed lock prevents conflicts
- **Partial bulk delete**: Counts successful deletions accurately

## Performance Considerations

### Single Delete
- **Before**: 1 query (direct delete)
- **After**: 2 queries (load + cancel if needed + delete)
- **Impact**: Minimal - proper coordination is worth the extra query

### Bulk Delete
- **Running instances**: N queries (where N = number of running instances)
  - Acceptable because running instances are typically few
  - Proper coordination requires individual handling
- **Finished instances**: 1 query (bulk delete)
  - Maintains performance for the common case
  - Most instances are finished, not running

### Optimization Opportunities
- Could add a `BulkCancelAsync` method for running instances
- Could batch running instance deletions with parallel processing
- Current approach prioritizes correctness over optimization

## Migration Notes

### Breaking Changes
None - this is a bug fix that doesn't change the API surface.

### Behavioral Changes
1. **Running workflows are now cancelled before deletion**
   - Previous behavior: Direct delete (potentially inconsistent state)
   - New behavior: Cancel then delete (proper cleanup)

2. **Bulk delete now handles running vs finished instances differently**
   - Previous behavior: Attempted bulk delete of all instances
   - New behavior: Individual delete for running, bulk for finished

### Upgrade Path
No action required - changes are fully backward compatible.

## Files Modified

1. `src/modules/Elsa.Workflows.Runtime/Contracts/IWorkflowClient.cs`
   - Added `DeleteAsync()` method

2. `src/modules/Elsa.Workflows.Runtime/Services/LocalWorkflowClient.cs`
   - Implemented `DeleteAsync()` with proper cancellation
   - Refactored `CancelAsync()` to avoid duplicate loading
   - Added `TryGetWorkflowInstanceAsync()` helper

3. `src/modules/Elsa.Workflows.Runtime.Distributed/Services/DistributedWorkflowClient.cs`
   - Implemented `DeleteAsync()` with distributed locking

4. `src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances/Delete/Endpoint.cs`
   - Changed to use `IWorkflowRuntime` instead of `IWorkflowInstanceManager`
   - Uses workflow client for deletion

5. `src/modules/Elsa.Workflows.Api/Endpoints/WorkflowInstances/BulkDelete/Endpoint.cs`
   - Refactored to handle running and finished instances separately
   - Changed to use `IWorkflowRuntime` for running instances
   - Maintains bulk delete for finished instances

## Related Issues

- **Bug #7077**: Workflow instance not being deleted
- Related to proper runtime coordination and distributed locking

## Future Enhancements

1. **Batch cancellation**: Add `BulkCancelAsync` for running instances
2. **Parallel deletion**: Process running instances in parallel with configurable concurrency
3. **Soft delete**: Add option for soft delete with retention period
4. **Delete callbacks**: Add hooks for cleanup of related resources
5. **Metrics**: Track deletion performance and success rates

---

## Key Takeaways

1. **Always use runtime abstractions**: Don't bypass `IWorkflowClient` to directly manipulate persistence
2. **Coordinate running workflows**: Active workflows need proper cancellation before deletion
3. **Use distributed locks**: Prevent race conditions in multi-node deployments
4. **Separate concerns by status**: Running and finished workflows have different coordination needs
5. **Performance vs correctness**: Prioritize correctness, optimize performance within those constraints
