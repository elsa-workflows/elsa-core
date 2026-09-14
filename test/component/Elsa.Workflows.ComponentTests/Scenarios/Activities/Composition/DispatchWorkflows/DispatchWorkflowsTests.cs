using Elsa.Common.Models;
using Elsa.Testing.Shared.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.DispatchWorkflows.Workflows;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.DispatchWorkflows.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.DispatchWorkflows;

public class DispatchWorkflowsTests : AppComponentTest
{
    private AsyncWorkflowRunner _workflowRunner = null!;
    private const int ExpectedWriteLineCount = 2; // One from parent, one from child
    private const int ChildWorkflowTimeoutSeconds = 30;

    public DispatchWorkflowsTests(App app) : base(app)
    {
    }

    protected override ValueTask OnInitializeAsync()
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
        return ValueTask.CompletedTask;
    }

    [Test]
    [DisplayName("DispatchWorkflow should wait for child workflow to complete")]
    public async Task DispatchAndWaitWorkflow_ShouldWaitForChildWorkflowToComplete()
    {
        var result = await RunWorkflowAsync(DispatchAndWaitWorkflow.DefinitionId);

        await AssertWorkflowFinished(result);
        var writeLineExecutionRecords = result.ActivityExecutionRecords.Where(x => x.ActivityType == "Elsa.WriteLine").ToList();
        await Assert.That(writeLineExecutionRecords.Count).IsEqualTo(ExpectedWriteLineCount);
    }

    [Test]
    [DisplayName("DispatchWorkflow should dispatch and not wait when WaitForCompletion is false")]
    public async Task DispatchFireAndForget_ShouldNotWaitForChildWorkflow()
    {
        // Run the main workflow and wait for child workflow to complete
        var (result, completedChildWorkflows) = await RunWorkflowAndWaitForChildWorkflowAsync(
            DispatchFireAndForgetWorkflow.DefinitionId,
            SlowChildWorkflow.DefinitionId);

        await AssertWorkflowFinished(result);
        var mainWorkflowCompletedAt = result.WorkflowExecutionContext.UpdatedAt;

        // Assert that the child workflow completed after the main workflow
        var childContext = await Assert.That(completedChildWorkflows).HasSingleItem();
        await Assert.That(childContext.UpdatedAt > mainWorkflowCompletedAt).IsTrue().Because($"Child workflow should complete after main workflow. Main: {mainWorkflowCompletedAt}, Child: {childContext.UpdatedAt}");
    }

    [Test]
    [DisplayName("DispatchWorkflow should send input to child workflow")]
    public async Task DispatchWithInput_ShouldSendInputToChildWorkflow()
    {
        var result = await RunWorkflowAsync(DispatchWithInputWorkflow.DefinitionId);

        await AssertWorkflowFinished(result);
        var writeLineExecutionRecords = result.ActivityExecutionRecords.Where(x => x.ActivityType == "Elsa.WriteLine").ToList();
        await Assert.That(writeLineExecutionRecords.Count).IsEqualTo(ExpectedWriteLineCount);

        var writtenTexts = writeLineExecutionRecords
            .Select(x => x.ActivityState?[nameof(WriteLine.Text)] as string ?? string.Empty)
            .ToList();

        await Assert.That(writtenTexts).Contains("Received: Hello from parent!");
        await Assert.That(writtenTexts).Contains("Parent completed");
    }

    [Test]
    [DisplayName("DispatchWorkflow should use CorrelationId")]
    public async Task DispatchWithCorrelationId_ShouldUseCorrelationId()
    {
        // Run the main workflow and wait for child workflow to complete
        var (result, completedChildWorkflows) = await RunWorkflowAndWaitForChildWorkflowAsync(
            DispatchWithCorrelationIdWorkflow.DefinitionId,
            ChildWorkflow.DefinitionId);

        await AssertWorkflowFinished(result);

        // Assert that the child workflow has the expected correlation ID
        var childContext = await Assert.That(completedChildWorkflows).HasSingleItem();
        await Assert.That(childContext.CorrelationId).IsEqualTo("test-correlation-id-123");
    }

    [Test]
    [DisplayName("DispatchWorkflow should throw when workflow definition not found")]
    public async Task DispatchWithInvalidWorkflowDefinitionId_ShouldThrow()
    {
        var result = await RunWorkflowAsync(DispatchInvalidDefinitionWorkflow.DefinitionId);
        await Assert.That(result.WorkflowExecutionContext.SubStatus).IsEqualTo(WorkflowSubStatus.Faulted);
    }

    private Task<TestWorkflowExecutionResult> RunWorkflowAsync(string workflowDefinitionId)
    {
        return _workflowRunner.RunAndAwaitWorkflowCompletionAsync(WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published));
    }

    private static async Task AssertWorkflowFinished(TestWorkflowExecutionResult result)
    {
        await Assert.That(result.WorkflowExecutionContext.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }

    private async Task<(TestWorkflowExecutionResult Result, List<WorkflowState> CompletedChildWorkflows)> RunWorkflowAndWaitForChildWorkflowAsync(
        string parentWorkflowDefinitionId,
        string childWorkflowDefinitionId)
    {
        var result = await RunWorkflowAsync(parentWorkflowDefinitionId);
        var completedChildWorkflows = await WaitForCompletedChildWorkflowsAsync(result.WorkflowExecutionContext.Id, childWorkflowDefinitionId);

        return (result, completedChildWorkflows);
    }

    private async Task<List<WorkflowState>> WaitForCompletedChildWorkflowsAsync(string parentWorkflowInstanceId, string childWorkflowDefinitionId)
    {
        var workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(ChildWorkflowTimeoutSeconds);

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            var completedChildWorkflows = await FindChildWorkflowStatesAsync(workflowInstanceStore, parentWorkflowInstanceId, childWorkflowDefinitionId, WorkflowStatus.Finished);

            if (completedChildWorkflows.Count > 0)
                return completedChildWorkflows;

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        var childWorkflows = await FindChildWorkflowStatesAsync(workflowInstanceStore, parentWorkflowInstanceId, childWorkflowDefinitionId);
        var observedStates = string.Join(", ", childWorkflows.Select(x => $"{x.Id}:{x.Status}:{x.SubStatus}"));
        throw new TimeoutException($"Expected a completed child workflow with definition ID {childWorkflowDefinitionId}, but observed none. Observed child workflow states: {observedStates}");
    }

    private static async Task<List<WorkflowState>> FindChildWorkflowStatesAsync(
        IWorkflowInstanceStore workflowInstanceStore,
        string parentWorkflowInstanceId,
        string childWorkflowDefinitionId,
        WorkflowStatus? workflowStatus = null)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = childWorkflowDefinitionId,
            ParentWorkflowInstanceIds = new[] { parentWorkflowInstanceId },
            WorkflowStatus = workflowStatus
        };
        var instances = await workflowInstanceStore.FindManyAsync(filter);
        return instances.Select(x => x.WorkflowState).ToList();
    }
}
