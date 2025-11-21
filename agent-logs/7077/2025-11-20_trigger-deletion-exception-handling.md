# Exception Handling Improvements for Trigger Deletion and Scheduled Workflows

**Date**: November 20, 2025
**Issue**: #7077
**Branch**: bug/7077
**Type**: Bug Fix + Integration Tests

## Problem

When deleting triggers, the `TriggerIndexer.DeleteTriggersAsync` method would fail completely if any workflow failed to load due to a missing materializer or other exceptions. This resulted in the error:

```
System.Exception: Provider not found
   at Elsa.Workflows.Management.Services.WorkflowDefinitionService.MaterializeWorkflowAsync
```

The method would stop processing remaining workflows, leaving triggers in an inconsistent state.

## Solution

### 1. Exception Handling (`src/modules/Elsa.Workflows.Runtime/Services/TriggerIndexer.cs`)

Added a try-catch block in the `DeleteTriggersAsync` method (lines 67-79):

```csharp
foreach (var workflowDefinitionVersionId in workflowDefinitionVersionIds)
{
    try
    {
        var workflowGraph = await _workflowDefinitionService.FindWorkflowGraphAsync(
            workflowDefinitionVersionId, cancellationToken);

        if (workflowGraph == null)
            continue;

        await DeleteTriggersAsync(workflowGraph.Workflow, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex,
            "Failed to load workflow graph for workflow definition version {WorkflowDefinitionVersionId}. " +
            "Skipping trigger deletion for this workflow.",
            workflowDefinitionVersionId);
    }
}
```

**Benefits**:
- Logs warning with full exception details for debugging
- Continues processing remaining workflows instead of failing completely
- Maintains system stability when individual workflows have issues

### 2. Integration Tests (`test/integration/Elsa.Workflows.IntegrationTests/Scenarios/TriggerIndexing/`)

Created comprehensive integration tests in a new `TriggerIndexing` scenario folder with `Tests.cs`:

#### Test 1: All Workflows Fail to Load
```csharp
[Fact(DisplayName = "DeleteTriggersAsync should continue deleting triggers even when workflow fails to load")]
public async Task DeleteTriggersAsync_WorkflowFailsToLoad_ContinuesWithOtherTriggers()
```

- Verifies the method completes without throwing when all workflows fail
- Tests multiple failure types (JSON serialization errors, provider not found)
- Confirms all triggers remain when workflows can't be loaded

#### Test 2: Mixed Success and Failure
```csharp
[Fact(DisplayName = "DeleteTriggersAsync processes all workflows even when one fails")]
public async Task DeleteTriggersAsync_OneWorkflowFails_ProcessesAllWorkflows()
```

- Tests scenario where one workflow fails (workflow2 with "Provider not found")
- Two other workflows load successfully (workflow1 and workflow3)
- Verifies triggers for successful workflows are deleted
- Confirms failed workflow's trigger remains
- **This is the key test** - proves exception handling allows processing to continue

#### Test Infrastructure

**Custom Materializers**:
- `FailingMaterializer`: Throws "Provider not found" exception to simulate the reported issue
- `WorkingMaterializer`: Successfully materializes workflows from a registry for valid test cases

**Helper Methods** (DRY principle):
- `CreateWorkflowDefinition()`: Factory method for workflow definitions
- `CreateTrigger()`: Factory method for triggers
- `SaveWorkflowsAndTriggersAsync()`: Common save and verification logic

**Modern C# Features**:
- `file` scoped classes for helper materializers (limits visibility)
- Primary constructors for `WorkingMaterializer`
- Expression-bodied members for simple methods

## Code Quality Improvements

### Test Refactoring
- Renamed from `TriggerIndexerTests.cs` to `Tests.cs` (project convention)
- Reduced test methods from ~90 lines to ~30 lines each
- Eliminated duplicate workflow/trigger creation code
- Improved readability with array-based test data
- Clear Arrange-Act-Assert structure

### Benefits
- **More maintainable**: Changes only need to be made in one place
- **More readable**: Test intent is clearer with less boilerplate
- **More consistent**: Follows project naming conventions
- **Equally functional**: All tests pass with same behavior

## Test Results

Both tests pass successfully:

```
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 304 ms

✓ DeleteTriggersAsync should continue deleting triggers even when workflow fails to load [153 ms]
✓ DeleteTriggersAsync processes all workflows even when one fails [49 ms]
```

## 3. Resume Workflow Task Exception Handling (`src/modules/Elsa.Scheduling/Tasks/ResumeWorkflowTask.cs`)

### Problem

When a scheduled workflow instance is deleted before its scheduled resume time, the `ResumeWorkflowTask` would throw an unhandled exception:

```
Elsa.Workflows.Runtime.Exceptions.WorkflowInstanceNotFoundException: Workflow instance not found.
   at Elsa.Workflows.Runtime.LocalWorkflowClient.GetWorkflowInstanceAsync
```

This would cause the scheduled task to fail, even though the workflow instance no longer exists and should simply be skipped.

### Solution

Added exception handling to gracefully handle missing workflow instances:

```csharp
public async ValueTask ExecuteAsync(TaskExecutionContext context)
{
    var cancellationToken = context.CancellationToken;
    var workflowRuntime = context.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
    var logger = context.ServiceProvider.GetRequiredService<ILogger<ResumeWorkflowTask>>();

    try
    {
        var workflowClient = await workflowRuntime.CreateClientAsync(_request.WorkflowInstanceId, cancellationToken);
        var request = new RunWorkflowInstanceRequest
        {
            Input = _request.Input,
            Properties = _request.Properties,
            ActivityHandle = _request.ActivityHandle,
            BookmarkId = _request.BookmarkId
        };
        await workflowClient.RunInstanceAsync(request, cancellationToken);
    }
    catch (WorkflowInstanceNotFoundException ex)
    {
        logger.LogWarning(
            "Scheduled workflow instance {WorkflowInstanceId} no longer exists and was likely deleted. Skipping execution.",
            ex.InstanceId);
    }
}
```

**Benefits**:
- Scheduled tasks no longer fail when workflow instances are deleted
- Clear logging explains why the task was skipped
- System continues operating normally
- No unnecessary error noise in logs

## 4. LocalScheduler Thread Safety (`src/modules/Elsa.Scheduling/Services/LocalScheduler.cs`)

### Problem

The `LocalScheduler` was experiencing race conditions during concurrent access, particularly during application startup when multiple workflows are being scheduled simultaneously:

```
System.IndexOutOfRangeException: Index was outside the bounds of the array.
   at System.Collections.Generic.Dictionary`2.TryInsert(TKey key, TValue value, InsertionBehavior behavior)
   at System.Collections.Generic.Dictionary`2.set_Item(TKey key, TValue value)
   at Elsa.Scheduling.Services.LocalScheduler.RegisterScheduledTask
```

This exception occurs when multiple threads modify the internal `Dictionary` instances concurrently without synchronization.

### Root Cause

Two dictionaries were being accessed concurrently without any thread safety:
- `_scheduledTasks`: Maps task names to scheduled tasks
- `_scheduledTaskKeys`: Maps scheduled tasks to their keys

Methods like `ScheduleAsync`, `ClearScheduleAsync`, and internal helper methods all modified these dictionaries without locks.

### Solution

Added thread synchronization using a lock object:

```csharp
public class LocalScheduler : IScheduler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDictionary<string, IScheduledTask> _scheduledTasks = new Dictionary<string, IScheduledTask>();
    private readonly IDictionary<IScheduledTask, ICollection<string>> _scheduledTaskKeys = new Dictionary<IScheduledTask, ICollection<string>>();
    private readonly object _lock = new();  // Added lock object

    public ValueTask ScheduleAsync(string name, ITask task, ISchedule schedule, IEnumerable<string>? keys = null, CancellationToken cancellationToken = default)
    {
        var scheduleContext = new ScheduleContext(_serviceProvider, task);
        var scheduledTask = schedule.Schedule(scheduleContext);

        lock (_lock)  // Protected dictionary access
        {
            RegisterScheduledTask(name, scheduledTask, keys);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ClearScheduleAsync(string name, CancellationToken cancellationToken = default)
    {
        lock (_lock)  // Protected dictionary access
        {
            RemoveScheduledTask(name);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask ClearScheduleAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        lock (_lock)  // Protected dictionary access
        {
            RemoveScheduledTasks(keys);
        }

        return ValueTask.CompletedTask;
    }
}
```

**Benefits**:
- Eliminates race conditions during concurrent scheduling
- Prevents `IndexOutOfRangeException` during dictionary modifications
- Ensures thread-safe access to internal collections
- Simple and efficient locking strategy

**Implementation Notes**:
- Lock is only held during dictionary operations (minimal lock duration)
- Schedule creation happens outside the lock to minimize contention
- Private methods (`RegisterScheduledTask`, `RemoveScheduledTask`, `RemoveScheduledTasks`) are called within locks, so they don't need additional synchronization

**Why `lock` instead of `SemaphoreSlim`?**

The implementation uses a simple `lock` rather than `SemaphoreSlim` for optimal performance:

| Aspect | lock | SemaphoreSlim |
|--------|------|---------------|
| **Overhead** | Zero allocation | Heap allocations |
| **Complexity** | Automatic release | Manual try/finally |
| **Use Case** | Perfect for synchronous ops | Better for async ops |
| **Performance** | Compiler optimized | Additional overhead |
| **Async Support** | Cannot cross await | Can use WaitAsync |

**Decision Rationale**:
1. ✅ **All critical sections are synchronous** - Only dictionary operations (add/remove/lookup)
2. ✅ **Methods return `ValueTask.CompletedTask`** - Not truly async, just implementing interface signature
3. ✅ **No await inside critical sections** - Schedule creation happens outside the lock
4. ✅ **Zero allocation overhead** - `lock` is ideal for fast synchronous operations

`SemaphoreSlim` would be appropriate if:
- Critical sections contained `await` statements
- Methods were truly asynchronous with I/O operations
- Cancellation support was needed during lock acquisition

For this use case, `lock` is the optimal choice for simplicity, performance, and correctness.

## Files Changed

- `src/modules/Elsa.Workflows.Runtime/Services/TriggerIndexer.cs` - Added exception handling for workflow loading failures
- `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/TriggerIndexing/Tests.cs` - New integration tests
- `src/modules/Elsa.Scheduling/Tasks/ResumeWorkflowTask.cs` - Added exception handling for missing workflow instances
- `src/modules/Elsa.Scheduling/Services/LocalScheduler.cs` - Added thread synchronization for concurrent access

## Impact

- **Stability**: System no longer fails when workflows or instances have issues
- **Thread Safety**: LocalScheduler now handles concurrent scheduling operations safely
- **Observability**: Warnings are logged with clear explanations for workflow and instance issues
- **Reliability**: Other workflows and scheduled tasks continue to be processed correctly
- **Coverage**: Comprehensive tests ensure trigger deletion behavior is maintained
- **Resilience**:
  - Scheduled tasks handle deleted workflow instances gracefully
  - Scheduler handles concurrent access during startup and runtime
