using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Elsa.Workflows.Runtime.Messages;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>
/// What happens to bulk-dispatched child workflows when the <c>BulkDispatchWorkflows</c> activity waiting for them is cancelled.
/// </summary>
public class Tests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder => builder
    .AddWorkflow<BlockingChildWorkflow>()
    .AddWorkflow<FireAndForgetChildWorkflow>()
    .AddWorkflow<SecondBulkChildWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitInForkWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitBesideSiblingDispatchesWorkflow>())
{
    [Fact(DisplayName = "Cancelling a waiting BulkDispatchWorkflows activity cancels all the child workflows it dispatched, past the first page")]
    public async Task CancellingTheWaitingBulkDispatchActivity_CancelsTheChildWorkflows()
    {
        const int childCount = BulkDispatchAndWaitInForkWorkflow.ChildCount;

        // Arrange: the children are dispatched and suspended before the parent moves on.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitInForkWorkflow));
        var childIds = (await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsSuspended)).Select(x => x.Id).Order().ToList();

        // Act: the other fork branch completes, so the fork completes and cancels the waiting BulkDispatchWorkflows.
        var resumed = await ResumeProceedAsync(parentId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);

        // Assert: a cancellation was dispatched for every child, and they land. What is asserted is the status the
        // children end up in, so an asynchronous cancellation gets the same time a dispatch gets.
        Assert.Equal(childIds, CancellationTargets.Order());
        var children = await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsFinished, throwOnTimeout: false);

        Assert.All(children, child =>
        {
            Assert.Equal(WorkflowStatus.Finished, child.Status);
            Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus);
        });
    }

    [Fact(DisplayName = "Cancelling a waiting BulkDispatchWorkflows activity leaves fire-and-forget and other bulk dispatches of the same parent running")]
    public async Task CancellingTheWaitingBulkDispatchActivity_LeavesSiblingDispatchesRunning()
    {
        const int waitedChildCount = BulkDispatchAndWaitBesideSiblingDispatchesWorkflow.WaitedChildCount;

        // Arrange: the parent has dispatched one child fire-and-forget, and two bulk activities are waiting for their own children.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitBesideSiblingDispatchesWorkflow));
        await WaitForChildAsync(parentId, nameof(FireAndForgetChildWorkflow), IsSuspended);
        await WaitForChildAsync(parentId, nameof(SecondBulkChildWorkflow), IsSuspended);
        var waitedChildIds = (await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), waitedChildCount, IsSuspended)).Select(x => x.Id).Order().ToList();

        // Act: cancel the first bulk activity. The second one keeps the parent waiting.
        var resumed = await ResumeProceedAsync(parentId);
        Assert.Equal(WorkflowSubStatus.Suspended, resumed.SubStatus);

        // Assert: the cancelled activity dispatched a cancellation for each child it was waiting for and nothing else,
        // and those cancellations land.
        Assert.Equal(waitedChildIds, CancellationTargets.Order());
        var waitedChildren = await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), waitedChildCount, IsFinished);
        Assert.All(waitedChildren, child => Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus));
    }

    /// <summary>Resumes the parent's proceed branch, which completes the inner fork and cancels its waiting bulk activity.</summary>
    private Task<RunWorkflowInstanceResponse> ResumeProceedAsync(string parentId) => ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
}
