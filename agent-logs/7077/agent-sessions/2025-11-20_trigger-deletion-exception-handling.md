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

## Files Changed

- `src/modules/Elsa.Workflows.Runtime/Services/TriggerIndexer.cs` - Added exception handling
- `test/integration/Elsa.Workflows.IntegrationTests/Scenarios/TriggerIndexing/Tests.cs` - New integration tests
- `src/modules/Elsa.Scheduling/Tasks/ResumeWorkflowTask.cs` - Added exception handling for missing workflow instances

## Impact

- **Stability**: System no longer fails when workflows or instances have issues
- **Observability**: Warnings are logged with clear explanations for both scenarios
- **Reliability**: Other workflows and scheduled tasks continue to be processed correctly
- **Coverage**: Comprehensive tests ensure trigger deletion behavior is maintained
- **Resilience**: Scheduled tasks handle deleted workflow instances gracefully
