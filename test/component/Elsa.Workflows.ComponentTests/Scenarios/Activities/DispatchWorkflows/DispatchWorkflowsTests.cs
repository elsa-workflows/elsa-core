using Elsa.Common.Models;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.Activities;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Composition.DispatchWorkflows.Workflows;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.DispatchWorkflows.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.DispatchWorkflows;

public class DispatchWorkflowsTests : AppComponentTest
{
    private readonly AsyncWorkflowRunner _workflowRunner;
    private const int ExpectedWriteLineCount = 2; // One from parent, one from child
    private const int ChildWorkflowTimeoutSeconds = 30;

    public DispatchWorkflowsTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
    }

    [Fact(DisplayName = "DispatchWorkflow should wait for child workflow to complete")]
    public async Task DispatchAndWaitWorkflow_ShouldWaitForChildWorkflowToComplete()
    {
        var result = await RunWorkflowAsync(DispatchAndWaitWorkflow.DefinitionId);

        AssertWorkflowFinished(result);
        var writeLineExecutionRecords = result.ActivityExecutionRecords.Where(x => x.ActivityType == "Elsa.WriteLine").ToList();
        Assert.Equal(ExpectedWriteLineCount, writeLineExecutionRecords.Count);
    }

    [Fact(DisplayName = "DispatchWorkflow should dispatch and not wait when WaitForCompletion is false")]
    public async Task DispatchFireAndForget_ShouldNotWaitForChildWorkflow()
    {
        // Run the main workflow and wait for child workflow to complete
        var (result, completedChildWorkflows) = await RunWorkflowAndWaitForChildWorkflowAsync(
            DispatchFireAndForgetWorkflow.DefinitionId,
            SlowChildWorkflow.DefinitionId);

        AssertWorkflowFinished(result);
        var mainWorkflowCompletedAt = result.WorkflowExecutionContext.UpdatedAt;

        // Assert that the child workflow completed after the main workflow
        var childContext = Assert.Single(completedChildWorkflows);
        Assert.True(childContext.UpdatedAt > mainWorkflowCompletedAt,
            $"Child workflow should complete after main workflow. Main: {mainWorkflowCompletedAt}, Child: {childContext.UpdatedAt}");
    }

    [Fact(DisplayName = "DispatchWorkflow should send input to child workflow")]
    public async Task DispatchWithInput_ShouldSendInputToChildWorkflow()
    {
        var result = await RunWorkflowAsync(DispatchWithInputWorkflow.DefinitionId);

        AssertWorkflowFinished(result);
        var writeLineExecutionRecords = result.ActivityExecutionRecords.Where(x => x.ActivityType == "Elsa.WriteLine").ToList();
        Assert.Equal(ExpectedWriteLineCount, writeLineExecutionRecords.Count);

        var writtenTexts = writeLineExecutionRecords
            .Select(x => x.ActivityState?[nameof(WriteLine.Text)] as string ?? string.Empty)
            .ToList();

        Assert.Contains("Received: Hello from parent!", writtenTexts);
        Assert.Contains("Parent completed", writtenTexts);
    }

    [Fact(DisplayName = "DispatchWorkflow should use CorrelationId")]
    public async Task DispatchWithCorrelationId_ShouldUseCorrelationId()
    {
        // Run the main workflow and wait for child workflow to complete
        var (result, completedChildWorkflows) = await RunWorkflowAndWaitForChildWorkflowAsync(
            DispatchWithCorrelationIdWorkflow.DefinitionId,
            ChildWorkflow.DefinitionId);

        AssertWorkflowFinished(result);

        // Assert that the child workflow has the expected correlation ID
        var childContext = Assert.Single(completedChildWorkflows);
        Assert.Equal("test-correlation-id-123", childContext.CorrelationId);
    }

    [Fact(DisplayName = "DispatchWorkflow should throw when workflow definition not found")]
    public async Task DispatchWithInvalidWorkflowDefinitionId_ShouldThrow()
    {
        var result = await RunWorkflowAsync(DispatchInvalidDefinitionWorkflow.DefinitionId);
        Assert.Equal(WorkflowSubStatus.Faulted, result.WorkflowExecutionContext.SubStatus);
    }

    private Task<TestWorkflowExecutionResult> RunWorkflowAsync(string workflowDefinitionId)
    {
        return _workflowRunner.RunAndAwaitWorkflowCompletionAsync(WorkflowDefinitionHandle.ByDefinitionId(workflowDefinitionId, VersionOptions.Published));
    }

    private static void AssertWorkflowFinished(TestWorkflowExecutionResult result)
    {
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowExecutionContext.SubStatus);
    }

    private async Task<(TestWorkflowExecutionResult Result, List<WorkflowExecutionContext> CompletedChildWorkflows)> RunWorkflowAndWaitForChildWorkflowAsync(
        string parentWorkflowDefinitionId,
        string childWorkflowDefinitionId)
    {
        var workflowEvents = Scope.ServiceProvider.GetRequiredService<WorkflowEvents>();
        var completedChildWorkflows = new List<WorkflowExecutionContext>();
        var childWorkflowCompletionTcs = new TaskCompletionSource();

        // Subscribe to child workflow completion events
        void OnWorkflowStateCommitted(object? sender, WorkflowStateCommittedEventArgs e)
        {
            if (e.WorkflowExecutionContext.Workflow.Identity.DefinitionId != childWorkflowDefinitionId ||
                e.WorkflowExecutionContext.Status != WorkflowStatus.Finished)
            {
                return;
            }

            completedChildWorkflows.Add(e.WorkflowExecutionContext);
            childWorkflowCompletionTcs.TrySetResult();
        }

        workflowEvents.WorkflowStateCommitted += OnWorkflowStateCommitted;

        try
        {
            // Run the main workflow
            var result = await RunWorkflowAsync(parentWorkflowDefinitionId);

            // Wait for the child workflow to complete
            await childWorkflowCompletionTcs.Task.WaitAsync(TimeSpan.FromSeconds(ChildWorkflowTimeoutSeconds));

            return (result, completedChildWorkflows);
        }
        finally
        {
            workflowEvents.WorkflowStateCommitted -= OnWorkflowStateCommitted;
        }
    }
}
