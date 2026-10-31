using Elsa.Workflows.IntegrationTests.Scenarios.DispatchWorkflowCancellation;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.BulkDispatchWorkflowsCancellation;

/// <summary>
/// What happens to bulk-dispatched child workflows when the <c>BulkDispatchWorkflows</c> activity waiting for them is cancelled.
/// </summary>
public class Tests(ITestOutputHelper testOutputHelper) : ChildWorkflowCancellationTests(testOutputHelper, builder => builder
    .AddWorkflow<BlockingChildWorkflow>()
    .AddWorkflow<FireAndForgetChildWorkflow>()
    .AddWorkflow<FinishingChildWorkflow>()
    .AddWorkflow<SecondBulkChildWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitInForkWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitForFinishingChildWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitBesideSiblingDispatchesWorkflow>()
    .AddWorkflow<BulkDispatchAndWaitWorkflow>())
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

    [Fact(DisplayName = "Cancelling a waiting BulkDispatchWorkflows activity skips a child that has already finished")]
    public async Task CancellingTheWaitingBulkDispatchActivity_WhenAChildAlreadyFinished_SkipsThatChild()
    {
        // Arrange: the child has dispatched a fire-and-forget child of its own, and then finished. The test host runs no
        // bookmark queue worker, so the child's completion is not delivered and the parent's BulkDispatchWorkflows keeps waiting.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitForFinishingChildWorkflow));
        var childId = (await WaitForChildAsync(parentId, nameof(FinishingChildWorkflow), IsSuspended)).Id;
        await WaitForChildAsync(childId, nameof(FireAndForgetChildWorkflow), IsSuspended);

        var finished = await ResumeAsync(childId, FinishingChildWorkflow.FinishActivityId);
        Assert.Equal(WorkflowSubStatus.Finished, finished.SubStatus);

        // Act: the other fork branch completes, so the fork completes and cancels the waiting BulkDispatchWorkflows.
        var resumed = await ResumeProceedAsync(parentId);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);

        // Assert: the parent finished without an incident, and no cancellation was dispatched, so neither the finished
        // child nor its own child is touched.
        Assert.Empty((await ExportStateAsync(parentId)).Incidents);
        Assert.Empty(CancellationTargets);
    }

    [Fact(DisplayName = "Cancelling waited-for children through the cancellation service resumes their parent once every child is counted")]
    public async Task CancellingTheWaitedForChildren_ResumesTheParent()
    {
        const int childCount = BulkDispatchAndWaitWorkflow.ChildCount;

        // Arrange: the parent is waiting for its suspended children.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitWorkflow));
        var childIds = (await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsSuspended)).Select(x => x.Id).Order().ToList();
        var cancellationService = Services.GetRequiredService<IWorkflowCancellationService>();

        // Act: cancel the first child, the way the cancellation API does. The test host runs no bookmark queue worker, so
        // deliver the resume the child's cancellation queued for its parent.
        Assert.True(await cancellationService.CancelWorkflowAsync(childIds[0]));
        await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, child => child.Id != childIds[0] || IsFinished(child));
        await Services.GetRequiredService<IBookmarkQueueProcessor>().ProcessAsync();

        // Assert: the cancelled child counted as one finished child, so the parent still waits for the other one.
        Assert.Equal(WorkflowSubStatus.Suspended, (await ExportStateAsync(parentId)).SubStatus);

        // Act: cancel the second child too.
        Assert.True(await cancellationService.CancelWorkflowAsync(childIds[1]));
        await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsFinished);
        await Services.GetRequiredService<IBookmarkQueueProcessor>().ProcessAsync();

        // Assert: both children were cancelled, and the parent's BulkDispatchWorkflows completed, as it does for children
        // that finish, so the parent finished.
        var children = await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), childCount, IsFinished);
        Assert.All(children, child => Assert.Equal(WorkflowSubStatus.Cancelled, child.SubStatus));
        var parent = await ExportStateAsync(parentId);
        Assert.Equal(WorkflowStatus.Finished, parent.Status);
        Assert.Equal(WorkflowSubStatus.Finished, parent.SubStatus);
        Assert.Empty(parent.Incidents);
    }

    [Fact(DisplayName = "The children a cancelled BulkDispatchWorkflows activity cancels do not resume a sibling BulkDispatchWorkflows of the same parent")]
    public async Task CancellingTheWaitingBulkDispatchActivity_DoesNotResumeASiblingBulkDispatch()
    {
        const int waitedChildCount = BulkDispatchAndWaitBesideSiblingDispatchesWorkflow.WaitedChildCount;

        // Arrange: two bulk activities of the same parent are waiting for their own children.
        var parentId = await StartParentAsync(nameof(BulkDispatchAndWaitBesideSiblingDispatchesWorkflow));
        await WaitForChildAsync(parentId, nameof(SecondBulkChildWorkflow), IsSuspended);
        await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), waitedChildCount, IsSuspended);

        // Act: cancel the first bulk activity, which cancels its children, then deliver whatever resumes those
        // cancellations queued. The test host runs no bookmark queue worker.
        var resumed = await ResumeProceedAsync(parentId);
        Assert.Equal(WorkflowSubStatus.Suspended, resumed.SubStatus);
        await WaitForChildrenAsync(parentId, nameof(BlockingChildWorkflow), waitedChildCount, IsFinished);
        await Services.GetRequiredService<IBookmarkQueueProcessor>().ProcessAsync();

        // Assert: the bulk activity of the same parent that waits for its own still-running child is still waiting.
        var parent = await ExportStateAsync(parentId);
        Assert.Equal(WorkflowSubStatus.Suspended, parent.SubStatus);
        Assert.Empty(parent.Incidents);
    }

    /// <summary>Resumes the parent's proceed branch, which completes the inner fork and cancels its waiting bulk activity.</summary>
    private Task<RunWorkflowInstanceResponse> ResumeProceedAsync(string parentId) => ResumeAsync(parentId, BulkDispatchAndWaitInForkWorkflow.ProceedActivityId);
}
