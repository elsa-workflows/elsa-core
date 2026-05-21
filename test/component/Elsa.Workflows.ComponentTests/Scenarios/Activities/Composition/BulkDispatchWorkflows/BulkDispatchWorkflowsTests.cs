using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.BulkDispatchWorkflows.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.BulkDispatchWorkflows;

public class BulkDispatchWorkflowsTests : AppComponentTest
{
    private const int ChildWorkflowTimeoutSeconds = 30;
    private readonly AsyncWorkflowRunner _workflowRunner;

    public BulkDispatchWorkflowsTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should wait for all child workflows to complete")]
    public async Task BulkDispatchAndWait_ShouldWaitForAllChildWorkflowsToComplete()
    {
        var result = await RunWorkflowAsync(BulkDispatchAndWaitWorkflow.DefinitionId);

        var writeLineExecutionRecords = result.ActivityExecutionRecords.Where(x => x.ActivityType == "Elsa.WriteLine").ToList();
        Assert.Equal(4, writeLineExecutionRecords.Count);
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should dispatch and not wait when WaitForCompletion is false")]
    public async Task BulkDispatchFireAndForget_ShouldNotWaitForChildWorkflows()
    {
        var expectedChildCount = 3;

        // Run the main workflow and wait for child workflows to complete
        var (result, completedChildWorkflows) = await RunWorkflowAndWaitForChildWorkflowsAsync(
            BulkDispatchFireAndForgetWorkflow.DefinitionId,
            SlowBulkChildWorkflow.DefinitionId,
            expectedChildCount);

        AssertWorkflowFinished(result);
        var mainWorkflowCompletedAt = result.WorkflowExecutionContext.UpdatedAt;

        // Assert that all child workflows completed after the main workflow
        Assert.Equal(expectedChildCount, completedChildWorkflows.Count);
        foreach (var childContext in completedChildWorkflows)
        {
            Assert.True(childContext.UpdatedAt > mainWorkflowCompletedAt,
                $"Child workflow should complete after main workflow. Main: {mainWorkflowCompletedAt}, Child: {childContext.UpdatedAt}");
        }
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should use CorrelationIdFunction")]
    public async Task BulkDispatchWithCorrelationId_ShouldUseCorrelationIdFunction()
    {
        var expectedChildCount = 3;

        // Run the main workflow and wait for child workflows to complete
        var (result, completedChildWorkflows) = await RunWorkflowAndWaitForChildWorkflowsAsync(
            BulkDispatchWithCorrelationIdWorkflow.DefinitionId,
            BulkChildWorkflow.DefinitionId,
            expectedChildCount);

        AssertWorkflowFinished(result);

        // Assert that all child workflows have the expected correlation IDs based on the CorrelationIdFunction
        Assert.Equal(expectedChildCount, completedChildWorkflows.Count);

        var expectedCorrelationIds = new[] { "correlation-1", "correlation-2", "correlation-3" };
        var actualCorrelationIds = completedChildWorkflows.Select(c => c.CorrelationId).OrderBy(c => c).ToList();

        Assert.Equal(expectedCorrelationIds, actualCorrelationIds);
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should execute ChildFaulted ports")]
    public async Task BulkDispatchWithChildPorts_ShouldExecuteChildFaultedPortForFaultedWorkflows()
    {
        var result = await RunWorkflowAsync(BulkDispatchWithBulkChildPortsWorkflow.DefinitionId);
        AssertWorkflowFinished(result);

        var faultedCount = await GetWorkflowVariableAsync<int>(result, "FaultedCount");
        Assert.Equal(3, faultedCount);
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should complete immediately when Items is empty")]
    public async Task BulkDispatchWithEmptyItems_ShouldCompleteImmediately()
    {
        var result = await RunWorkflowAsync(BulkDispatchEmptyItemsWorkflow.DefinitionId);
        AssertWorkflowFinished(result);
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should throw when workflow definition not found")]
    public async Task BulkDispatchWithInvalidWorkflowDefinitionId_ShouldThrow()
    {
        var result = await RunWorkflowAsync(BulkDispatchInvalidDefinitionWorkflow.DefinitionId);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowExecutionContext.SubStatus);
    }

    [Fact(DisplayName = "BulkDispatchWorkflows child workflows should receive current item")]
    public async Task BulkDispatchWorkflows_ChildWorkflowsShouldReceiveCurrentItem()
    {
        var result = await RunWorkflowAsync(MixFruitsWorkflow.DefinitionId);
        AssertWorkflowFinished(result);

        var writeLineExecutionRecords = result.ActivityExecutionRecords.Where(x => x.ActivityType == "Elsa.WriteLine").ToList();
        Assert.Equal(3, writeLineExecutionRecords.Count);

        var writtenTexts = writeLineExecutionRecords
            .Select(x => x.ActivityState?[nameof(WriteLine.Text)] as string)
            .ToList();

        Assert.Contains("Mixing Apple", writtenTexts);
        Assert.Contains("Mixing Banana", writtenTexts);
        Assert.Contains("Mixing Cherry", writtenTexts);
    }

    private Task<TestWorkflowExecutionResult> RunWorkflowAsync(string workflowDefinitionId)
    {
        return _workflowRunner.RunAndAwaitWorkflowCompletionAsync(WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published));
    }

    private static void AssertWorkflowFinished(TestWorkflowExecutionResult result)
    {
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowExecutionContext.SubStatus);
    }

    private async Task<T?> GetWorkflowVariableAsync<T>(TestWorkflowExecutionResult result, string variableName)
    {
        var variableManager = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceVariableManager>();
        var variables = await variableManager.GetVariablesAsync(result.WorkflowExecutionContext);
        return (T?)variables.FirstOrDefault(v => v.Variable.Name == variableName)?.Value;
    }

    private async Task<(TestWorkflowExecutionResult Result, List<WorkflowInstance> CompletedChildWorkflows)> RunWorkflowAndWaitForChildWorkflowsAsync(
        string parentWorkflowDefinitionId,
        string childWorkflowDefinitionId,
        int expectedChildCount)
    {
        var result = await RunWorkflowAsync(parentWorkflowDefinitionId);
        var completedChildWorkflows = await WaitForCompletedChildWorkflowInstancesAsync(result.WorkflowExecutionContext.Id, childWorkflowDefinitionId, expectedChildCount);
        return (result, completedChildWorkflows);
    }

    private async Task<List<WorkflowInstance>> WaitForCompletedChildWorkflowInstancesAsync(string parentWorkflowInstanceId, string childWorkflowDefinitionId, int expectedChildCount)
    {
        var workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(ChildWorkflowTimeoutSeconds);

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            var completedChildWorkflows = (await workflowInstanceStore.FindManyAsync(new WorkflowInstanceFilter
            {
                DefinitionId = childWorkflowDefinitionId,
                ParentWorkflowInstanceIds = [parentWorkflowInstanceId],
                WorkflowStatus = WorkflowStatus.Finished
            })).OrderBy(x => x.CreatedAt).ToList();

            if (completedChildWorkflows.Count == expectedChildCount)
                return completedChildWorkflows;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException($"Timed out waiting for {expectedChildCount} child workflows to complete.");
    }
}
