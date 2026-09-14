using Elsa.Common.Models;
using Elsa.Testing.Shared.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Finish.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Finish;

public class FinishTests : AppComponentTest
{
    private AsyncWorkflowRunner _workflowRunner = null!;

    public FinishTests(App app) : base(app)
    {
    }

    protected override ValueTask OnInitializeAsync()
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
        return ValueTask.CompletedTask;
    }

    [Test]
    public async Task Finish_SimpleWorkflow_MarksWorkflowAsFinished()
    {
        // Act
        var result = await RunWorkflowAsync(SimpleFinishWorkflow.DefinitionId);

        // Assert
        await AssertWorkflowFinished(result);
    }

    [Test]
    public async Task Finish_ClearsScheduler_NoScheduledActivities()
    {
        // Act
        var result = await RunWorkflowAsync(SimpleFinishWorkflow.DefinitionId);

        // Assert
        await Assert.That(result.WorkflowExecutionContext.Scheduler.List()).IsEmpty();
    }

    [Test]
    public async Task Finish_ClearsBookmarks_NoBookmarksRemain()
    {
        // Act
        var result = await RunWorkflowAsync(SimpleFinishWorkflow.DefinitionId);

        // Assert
        await Assert.That(result.WorkflowExecutionContext.Bookmarks).IsEmpty();
    }

    [Test]
    public async Task Finish_WithFork_ClearsCompletionCallbacks()
    {
        // Act - Workflow publishes an event, then forks into two branches waiting for events
        // One branch receives the event and completes (due to WaitAny join mode)
        // Finish then clears the remaining completion callbacks from the other branch
        var result = await RunWorkflowAsync(FinishWithForkWorkflow.DefinitionId);

        // Assert - Finish should have cleared all completion callbacks, bookmarks, and scheduler
        await AssertWorkflowFullyCleared(result);
    }

    [Test]
    public async Task Finish_WithMultipleBookmarks_ClearsAllBookmarksAndCallbacks()
    {
        // Act - Workflow publishes EventA, forks into 3 branches, one receives event and completes
        // Finish then clears remaining bookmarks/callbacks from the other two branches
        var result = await RunWorkflowAsync(FinishWithMultipleBookmarksWorkflow.DefinitionId);

        // Assert - All bookmarks and callbacks should be cleared
        await AssertWorkflowFinished(result);
        await Assert.That(result.WorkflowExecutionContext.Bookmarks).IsEmpty();
        await Assert.That(result.WorkflowExecutionContext.CompletionCallbacks).IsEmpty();
    }

    [Test]
    public async Task Finish_InParallelBranch_TerminatesEntireWorkflow()
    {
        // Act
        var result = await RunWorkflowAsync(FinishInParallelBranchWorkflow.DefinitionId);

        // Assert - Workflow should be finished (not suspended waiting for the other branch)
        await AssertWorkflowFullyCleared(result);
    }

    [Test]
    public async Task Finish_VerifiesAllClearingOperations_InSingleTest()
    {
        // Act - Run workflow demonstrating Finish's clearing behavior:
        // 1. Fork creates completion callbacks for multiple branches
        // 2. One branch completes, triggering Fork's WaitAny completion
        // 3. Finish executes and clears: completion callbacks, bookmarks, and scheduler
        var result = await RunWorkflowAsync(FinishWithForkWorkflow.DefinitionId);

        // Assert - Comprehensive verification of Finish behavior
        await AssertWorkflowFullyCleared(result);
    }

    private async Task<TestWorkflowExecutionResult> RunWorkflowAsync(string definitionId) =>
        await _workflowRunner.RunAndAwaitWorkflowCompletionAsync(
            WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published));

    private static async Task AssertWorkflowFinished(TestWorkflowExecutionResult result)
    {
        await Assert.That(result.WorkflowExecutionContext.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(result.WorkflowExecutionContext.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }

    private static async Task AssertWorkflowFullyCleared(TestWorkflowExecutionResult result)
    {
        await AssertWorkflowFinished(result);
        await Assert.That(result.WorkflowExecutionContext.Bookmarks).IsEmpty();
        await Assert.That(result.WorkflowExecutionContext.Scheduler.List()).IsEmpty();
        await Assert.That(result.WorkflowExecutionContext.CompletionCallbacks).IsEmpty();
    }
}
