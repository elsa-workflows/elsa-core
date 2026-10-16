using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;

/// <summary>
/// What happens to a dispatched child workflow when the <c>DispatchWorkflow</c> activity waiting for it is cancelled.
/// </summary>
public class Tests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder => builder
    .AddWorkflow<BlockingChildWorkflow>()
    .AddWorkflow<FireAndForgetChildWorkflow>()
    .AddWorkflow<FinishingChildWorkflow>()
    .AddWorkflow<DispatchAndWaitInForkWorkflow>()
    .AddWorkflow<DispatchAndWaitBesideFireAndForgetWorkflow>()
    .AddWorkflow<DispatchAndWaitForFinishingChildWorkflow>())
{
    [Fact(DisplayName = "Cancelling a waiting DispatchWorkflow activity cancels the child workflow it dispatched")]
    public async Task CancellingTheWaitingDispatchActivity_CancelsTheChildWorkflow()
    {
        // Arrange: the child is dispatched and suspended before the parent moves on.
        var parentId = await StartParentAsync(nameof(DispatchAndWaitInForkWorkflow));
        await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsSuspended);

        // Act: the other fork branch completes, so the fork completes and cancels the waiting DispatchWorkflow.
        await ProceedParentAsync(parentId);

        // Assert the behavior: the child the cancelled activity was waiting for is cancelled too. Give an asynchronous
        // cancellation the same time a dispatch gets; what is asserted is the status the child ends up in.
        var child = await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsFinished, throwOnTimeout: false);

        Assert.Equal(WorkflowStatus.Finished, child.Status);
        Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus);
    }

    [Fact(DisplayName = "Cancelling a waiting DispatchWorkflow activity leaves a fire-and-forget sibling dispatch running")]
    public async Task CancellingTheWaitingDispatchActivity_LeavesAFireAndForgetSiblingRunning()
    {
        // Arrange: the parent has dispatched one child fire-and-forget and is waiting for another.
        var parentId = await StartParentAsync(nameof(DispatchAndWaitBesideFireAndForgetWorkflow));
        await WaitForChildAsync(parentId, nameof(FireAndForgetChildWorkflow), IsSuspended);
        var waitedChildId = (await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsSuspended)).Id;

        // Act
        await ProceedParentAsync(parentId);

        // Assert: the cancelled activity dispatched a cancellation for the waited-for child and nothing else, and that
        // cancellation lands.
        Assert.Equal(new[] { waitedChildId }, CancellationTargets);
        var waitedChild = await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsFinished);
        Assert.Equal(WorkflowSubStatus.Cancelled, waitedChild.SubStatus);
    }

    [Fact(DisplayName = "Cancelling a waiting DispatchWorkflow activity whose child already finished changes nothing")]
    public async Task CancellingTheWaitingDispatchActivity_WhenTheChildAlreadyFinished_IsANoOp()
    {
        // Arrange: the child has dispatched a fire-and-forget child of its own, and then finished. The test host runs no
        // bookmark queue worker, so the child's completion is not delivered and the parent's DispatchWorkflow keeps waiting.
        var parentId = await StartParentAsync(nameof(DispatchAndWaitForFinishingChildWorkflow));
        var childId = (await WaitForChildAsync(parentId, nameof(FinishingChildWorkflow), IsSuspended)).Id;
        await WaitForChildAsync(childId, nameof(FireAndForgetChildWorkflow), IsSuspended);

        var finished = await ResumeAsync(childId, FinishingChildWorkflow.FinishActivityId);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);
        Assert.Contains((await ExportStateAsync(parentId)).Bookmarks, x => x.ActivityId == DispatchAndWaitInForkWorkflow.DispatchActivityId);

        // Act
        await ProceedParentAsync(parentId);

        // Assert: the parent finished without an incident, and no cancellation was dispatched at all, so neither the
        // finished child nor its own child is touched.
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);
        Assert.Empty(CancellationTargets);
    }

    [Fact(DisplayName = "Control: cancelling the parent workflow through the cancellation service cancels the dispatched child")]
    public async Task Control_CancellingTheParentWorkflow_CancelsTheChildWorkflow()
    {
        // Arrange: the same parent and child, with the child dispatched and suspended.
        var parentId = await StartParentAsync(nameof(DispatchAndWaitInForkWorkflow));
        await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsSuspended);

        // Act
        Assert.True(await Services.GetRequiredService<IWorkflowCancellationService>().CancelWorkflowAsync(parentId));

        // Assert: the cascade by parent instance id reaches the child.
        var child = await WaitForChildAsync(parentId, nameof(BlockingChildWorkflow), IsFinished);

        Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus);
    }

    /// <summary>
    /// Resumes the parent's second fork branch, which completes the fork and cancels its waiting DispatchWorkflow, and
    /// asserts the setup: the parent finished, and the DispatchWorkflow activity's bookmark went with it.
    /// </summary>
    private async Task ProceedParentAsync(string parentId)
    {
        var resumed = await ResumeAsync(parentId, DispatchAndWaitInForkWorkflow.ProceedActivityId);

        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
        Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
        Assert.Empty((await ExportStateAsync(parentId)).Bookmarks);
    }
}
