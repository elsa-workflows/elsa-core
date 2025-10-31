using Elsa.Common.Models;
using Elsa.Testing.Shared.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.BulkDispatch.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.BulkDispatch;

public class BulkDispatchWorkflowsTests : AppComponentTest
{
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
        var result = await RunWorkflowAsync(BulkDispatchFireAndForgetWorkflow.DefinitionId);
        AssertWorkflowFinished(result);
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should handle dictionary items")]
    public async Task BulkDispatchWithDictionaryItems_ShouldProcessDictionaries()
    {
        var result = await RunWorkflowAsync(BulkDispatchWithDictionaryItemsWorkflow.DefinitionId);
        AssertWorkflowFinished(result);
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should use CorrelationIdFunction")]
    public async Task BulkDispatchWithCorrelationId_ShouldUseCorrelationIdFunction()
    {
        var result = await RunWorkflowAsync(BulkDispatchWithCorrelationIdWorkflow.DefinitionId);
        AssertWorkflowFinished(result);
    }

    [Fact(DisplayName = "BulkDispatchWorkflows should execute ChildFaulted ports")]
    public async Task BulkDispatchWithChildPorts_ShouldExecuteChildFaultedPortForFaultedWorkflows()
    {
        var result = await RunWorkflowAsync(BulkDispatchWithChildPortsWorkflow.DefinitionId);
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

    private async Task<T> GetWorkflowVariableAsync<T>(TestWorkflowExecutionResult result, string variableName)
    {
        var variableManager = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceVariableManager>();
        var variables = await variableManager.GetVariablesAsync(result.WorkflowExecutionContext);
        return (T)variables.FirstOrDefault(v => v.Variable.Name == variableName)?.Value!;
    }
}